using System;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.WalletApi;
using Osma.Mobile.App.Services.Interfaces;
using AgentOptions = Hyperledger.Aries.Configuration.AgentOptions;

namespace Osma.Mobile.App.Services
{
    public class AgentContextProvider : ICustomAgentContextProvider
    {
        private readonly IWalletService _walletService;
        private readonly IPoolService _poolService;
        private readonly IProvisioningService _provisioningService;
        private readonly IKeyValueStoreService _keyValueStoreService;
        private readonly IAgent _agent;

        private const string AgentOptionsKey = "AgentOptions";

        private AgentOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Osma.Mobile.App.Services.AgentContextProvider" /> class.
        /// </summary>
        /// <param name="walletService">Wallet service.</param>
        /// <param name="poolService">The pool service.</param>
        /// <param name="provisioningService">The provisioning service.</param>
        /// <param name="keyValueStoreService">Key value store.</param>
        public AgentContextProvider(IWalletService walletService,
            IPoolService poolService,
            IProvisioningService provisioningService,
            IKeyValueStoreService keyValueStoreService,
            IAgent agent)
        {
            _poolService = poolService;
            _provisioningService = provisioningService;
            _walletService = walletService;
            _keyValueStoreService = keyValueStoreService;
            _agent = agent;

            if (_keyValueStoreService.KeyExists(AgentOptionsKey))
                _options = _keyValueStoreService.GetData<AgentOptions>(AgentOptionsKey);
        }

        public async Task<bool> CreateAgentAsync(AgentOptions options)
        {
#if __ANDROID__
            WalletConfiguration.WalletStorageConfiguration _storage = new WalletConfiguration.WalletStorageConfiguration { Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".indy_client") };
            options.WalletConfiguration.StorageConfiguration = _storage;
#endif
            await _provisioningService.ProvisionAgentAsync(new AgentOptions
            {
                WalletConfiguration = options.WalletConfiguration,
                WalletCredentials = options.WalletCredentials,
                AgentKeySeed = options.AgentKeySeed,
                EndpointUri = options.EndpointUri != null ? new Uri($"{options.EndpointUri}") : null,
                AgentName = options.AgentName,
                PassCode = options.PassCode
            }) ;

            await _keyValueStoreService.SetDataAsync(AgentOptionsKey, options);
            _options = options;

            await _poolService.CreatePoolAsync(_options.PoolName, _options.GenesisFilename);

            return true;
        }

        public bool AgentExists() => _options != null;
        public async Task<IAgentContext> GetContextAsync(params object[] args)
        {
            if (!AgentExists())//TODO uniform approach to error protection
                throw new Exception("Agent doesnt exist");

            Wallet wallet;
            try
            {
                wallet = await _walletService.GetWalletAsync(_options.WalletConfiguration, _options.WalletCredentials);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return new AgentContext
            {
                Did = _options.AgentDid,
                Wallet = wallet,
                Pool = new PoolAwaitable(() => _poolService.GetPoolAsync(
                    _options.PoolName, _options.ProtocolVersion))
            };
        }

        //TODO implement the getAgentSync method
        public Task<IAgent> GetAgentAsync(params object[] args)
        {
            return Task.FromResult(_agent);
        }
    }
}
