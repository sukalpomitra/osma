using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Models.Wallets;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Services.Models;
using Osma.Mobile.App.Views.Legal;
using Osma.Mobile.App.Views.UserRegistration;
using ReactiveUI;
using Xamarin.Forms;
using Plugin.Fingerprint;

namespace Osma.Mobile.App.ViewModels
{
    public class RegisterViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;

        public RegisterViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider) : base(
                                 nameof(RegisterViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            DetectBiometricCapability();
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
            _fullName = "";
            _passCode = "";
            //set Account Object to have the full Name. But figure where the account is beingcreated. 
        }

        #region Bindable Property
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }

        private string _passCode;
        public string PassCode
        {
            get => _passCode;
            set => this.RaiseAndSetIfChanged(ref _passCode, value);
        }

        private bool _noBiometric;
        public bool NoBiometric
        {
            get => _noBiometric;
            set => this.RaiseAndSetIfChanged(ref _noBiometric, value);
        }

        private bool _secured;
        public bool Secured
        {
            get => _secured;
            set => this.RaiseAndSetIfChanged(ref _secured, value);
        }

        #endregion

        private async Task DetectBiometricCapability()
        {
            _noBiometric = !(await CrossFingerprint.Current.IsAvailableAsync(true));
            _secured = false;
        }

        #region Bindable Commands
        public ICommand CreateWalletCommand => new Command(async () =>
        {
            if (await isAuthenticatedAsync())
            {
                var dialog = UserDialogs.Instance.Loading("Creating wallet");

                var genesisFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "pool_genesis.Remote.txn");

                //TODO this register VM will have far more logic around the registration complexities, i.e backupservices
                //suppling ownership info to the agent etc..
                var options = new AgentOptions
                {
                    PoolOptions = new PoolOptions
                    {
                        GenesisFilename = genesisFilePath,
                        PoolName = "EdgeAgentPoolConnection",
                        ProtocolVersion = 2
                    },
                    WalletOptions = new WalletOptions
                    {
                        WalletConfiguration = new WalletConfiguration { Id = Guid.NewGuid().ToString() },
                        WalletCredentials = new WalletCredentials { Key = "LocalWalletKey" }
                    },
                    Name = FullName
                };

                if (await _agentContextProvider.CreateAgentAsync(options))
                {
                    await NavigationService.NavigateToAsync<MainViewModel>();
                    dialog?.Hide();
                    dialog?.Dispose();
                }
                else
                {
                    dialog?.Hide();
                    dialog?.Dispose();
                    UserDialogs.Instance.Alert("Failed to create wallet!");
                }
            }
        });

        private async Task<bool> isAuthenticatedAsync()
        {
            var result = await CrossFingerprint.Current.IsAvailableAsync(true);
            bool authenticated = true;
            if (result)
            {
                var auth = await CrossFingerprint.Current.AuthenticateAsync("Please authenticate to proceed");
                if (!auth.Authenticated)
                {
                    authenticated = false;
                }
            }
            return authenticated;
        }

        //public ICommand OpenFullNamePageCommand => new Command(async () => await _navigationService.NavigateToAsync<FullNameViewModel>());
        #endregion
    }
}
