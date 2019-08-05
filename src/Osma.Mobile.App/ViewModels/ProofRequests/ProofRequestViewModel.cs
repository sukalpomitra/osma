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
using AgentFramework.Core.Models.Proofs;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofRequestViewModel : ABaseViewModel
    {
        private readonly ProofRecord _proof;

        private readonly IProofService _proofService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IMessageService _messageService;
        private readonly IEventAggregator _eventAggregator;

        public ProofRequestViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            ICustomAgentContextProvider agentContextProvider,
            IMessageService messageService,
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
        }

        private async Task AcceptProofRequest()
        {
            if (_proof.State != AgentFramework.Core.Models.Records.ProofState.Requested)
            {
                await DialogService.AlertAsync("Proof state should be " + AgentFramework.Core.Models.Records.ProofState.Requested.ToString());
                await NavigationService.PopModalAsync();
                return;
            }

            RequestedAttribute requestedAttribute = new RequestedAttribute();
            requestedAttribute.CredentialId = "8d1e9b21-0844-424f-9beb-a9e8028f906b";
            requestedAttribute.Revealed = true;
            requestedAttribute.Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            Dictionary<String, RequestedAttribute> map = new Dictionary<string, RequestedAttribute>();
            map.Add("2e8b99a1-3e70-4899-8542-f39c9b4aeb85", requestedAttribute);
            map.Add("2e63bcbe-c0c6-4915-934b-9ea4b0341cea", requestedAttribute);
            RequestedCredentials requestedCredentials = new RequestedCredentials();
            requestedCredentials.RequestedAttributes = map;
            var context = await _agentContextProvider.GetContextAsync();
            var (msg, rec) = await _proofService.CreateProofAsync(context, _proof.Id, requestedCredentials);
            _ = await _messageService.SendAsync(context.Wallet, msg, rec);

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });
            // TODO: proof request accept logic
            await NavigationService.PopModalAsync();
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

        private IEnumerable<ProofAttribute> _attributes;
        public IEnumerable<ProofAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        #endregion
    }
}
