using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Contracts;
using System.Threading.Tasks;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.ViewModels.ProofRequests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofRequestViewModel : ABaseViewModel
    {
        private readonly ProofRecord _proof;

        private readonly IProofService _proofService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IMessageService _messageService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICredentialService _credentialService;

        private IDictionary<string, bool> _proofAttributes = new Dictionary<string, bool>();
        private IDictionary<string, bool> _previousProofAttribute = new Dictionary<string, bool>();

        public ProofRequestViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            ICustomAgentContextProvider agentContextProvider,
            IMessageService messageService,
            ICredentialService credentialService,
            IEventAggregator eventAggregator,
            ProofRecord proof
        ) : base(
            nameof(ProofRequestViewModel),
            userDialogs,
            navigationService
        )
        {
            _proof = proof;
            _proofService = proofService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;
            _credentialService = credentialService;
            _eventAggregator = eventAggregator;

            JObject requestJson = (JObject)JsonConvert.DeserializeObject(_proof.RequestJson);
            ProofName = requestJson["name"]?.ToString();
            ProofVersion = "Version - " + requestJson["version"]?.ToString();
            ProofState = _proof.State.ToString();

            JObject attributes = (JObject)requestJson["requested_attributes"];
            List<string> keys = attributes?.Properties().Select(p => p.Name).ToList();
            Attributes = keys
              .Select(k =>
                  new ProofAttribute()
                  {
                      Name = attributes[k]["name"]?.ToString(),
                      Type = "Text"
                  })
             .ToList();

            IsFrameVisible = false;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await base.InitializeAsync(navigationData);
        }

        private async Task AcceptProofRequest()
        {
            // TODO: proof request accept logic
            await NavigationService.PopModalAsync();
        }

        private async Task LoadProofCredentials(ProofAttribute proofAttribute)
        {
            if (_previousProofAttribute.Any() && !_previousProofAttribute.ContainsKey(proofAttribute.Name))
                _proofAttributes[_previousProofAttribute.Keys.Single()] = false;

            if (!_proofAttributes.ContainsKey(proofAttribute.Name))
            {
                _proofAttributes.Add(proofAttribute.Name, true);
                _previousProofAttribute = new Dictionary<string, bool> { { proofAttribute.Name, true } };

                IsFrameVisible = true;
            }
            else
            {
                _proofAttributes[proofAttribute.Name] = !_proofAttributes[proofAttribute.Name];
                _previousProofAttribute = new Dictionary<string, bool> { { proofAttribute.Name, _proofAttributes[proofAttribute.Name] } };

                IsFrameVisible = _proofAttributes[proofAttribute.Name];
            }

            if (!IsFrameVisible) return;

            var context = await _agentContextProvider.GetContextAsync();
            var credentialsRecords = await _credentialService.ListAsync(context);

            ProofCredentials = credentialsRecords;
        }

        private async Task RejectProofRequest()
        {
            // TODO: proof request reject logic
            await NavigationService.PopModalAsync();
        }

        #region Bindable Command

        public ICommand NavigateBackCommand => new Command(async () => await NavigationService.PopModalAsync());

        public ICommand AcceptProofRequestCommand => new Command(async () => await AcceptProofRequest());

        public ICommand RejectProofRequestCommand => new Command(async () => await RejectProofRequest());

        public ICommand SelectProofAttributeCommand => new Command<ProofAttribute>(async (proofAttribute) =>
        {
            if (proofAttribute != null) await LoadProofCredentials(proofAttribute);
        });

        #endregion

        #region Bindable Properties

        private string _proofName;
        public string ProofName
        {
            get => _proofName;
            set => this.RaiseAndSetIfChanged(ref _proofName, value);
        }

        private string _proofState;
        public string ProofState
        {
            get => _proofState;
            set => this.RaiseAndSetIfChanged(ref _proofState, value);
        }

        private string _proofVersion;
        public string ProofVersion
        {
            get => _proofVersion;
            set => this.RaiseAndSetIfChanged(ref _proofVersion, value);
        }

        private bool _isFrameVisible;
        public bool IsFrameVisible
        {
            get => _isFrameVisible;
            set => this.RaiseAndSetIfChanged(ref _isFrameVisible, value);
        }

        private IEnumerable<ProofAttribute> _attributes;
        public IEnumerable<ProofAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        private IList<CredentialRecord> _proofCredentials;
        public IList<CredentialRecord> ProofCredentials
        {
            get => _proofCredentials;
            set => this.RaiseAndSetIfChanged(ref _proofCredentials, value);
        }

        #endregion
    }
}
