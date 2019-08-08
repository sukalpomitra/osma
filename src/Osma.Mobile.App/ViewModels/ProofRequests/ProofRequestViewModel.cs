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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AgentFramework.Core.Models.Proofs;
using Xamarin.Forms.Internals;
using System.Reactive.Linq;

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

        private readonly JObject _requestedAttributes;
        private readonly JObject _requestedPredicates;

        private readonly IDictionary<string, bool> _proofAttributes = new Dictionary<string, bool>();
        private IDictionary<string, bool> _previousProofAttribute = new Dictionary<string, bool>();

        private readonly IDictionary<string, bool> _proofAttributesRevealed = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _requestedAttributesRevealedMap = new Dictionary<string, bool>();

        private readonly Dictionary<string, RequestedAttribute> _requestedAttributesMap = new Dictionary<string, RequestedAttribute>();
        private readonly Dictionary<string, RequestedAttribute> _requestedPredicatesMap = new Dictionary<string, RequestedAttribute>();

        private readonly IList<string> _requestedAtrributesKeys = new List<string>();
        private readonly IList<string> _requestedPredicatesKeys = new List<string>();

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

            _requestedAttributes = (JObject)requestJson["requested_attributes"];
            _requestedPredicates = (JObject)requestJson["requested_predicates"];

            _requestedAtrributesKeys = _requestedAttributes?.Properties().Select(p => p.Name).ToList();
            _requestedPredicatesKeys = _requestedPredicates?.Properties().Select(p => p.Name).ToList();

            Attributes = _requestedAtrributesKeys
                .Select(k =>
                 new ProofAttribute()
                 {
                     Name = _requestedAttributes[k]["name"]?.ToString(),
                     Type = "Text",
                     IsRevealed = true,
                     IsNotPredicate = true
                 })
                 .ToList();

            _requestedPredicatesKeys.ForEach(pk => 
            {
                var pa = new ProofAttribute()
                {
                    Name = _requestedPredicates[pk]["name"]?.ToString(),
                    Type = "Text",
                    IsRevealed = false,
                    IsNotPredicate = false
                };
                Attributes.Add(pa);
            });

           _requestedAtrributesKeys.ForEach(k => _requestedAttributesRevealedMap.Add(k, true));
        }

        public override async Task InitializeAsync(object navigationData)
        {
            RefreshProofRequest();

            _ = _eventAggregator.GetEventByType<ApplicationEvent>()
                            .Where(_ => _.Type == ApplicationEventType.ProofRequestAtrributeUpdated)
                            .Subscribe(_ => RefreshProofRequest());

            await base.InitializeAsync(navigationData);
        }

        public void RefreshProofRequest()
        {
            RefreshingProofRequest = true;

            IsFrameVisible = false;

            RefreshingProofRequest = false;
        }

        private async Task AcceptProofRequest()
        {
            if (_proof.State != AgentFramework.Core.Models.Records.ProofState.Requested)
            {
                await DialogService.AlertAsync("Proof state should be " + AgentFramework.Core.Models.Records.ProofState.Requested.ToString());
                return;
            }

            if (_requestedAttributesMap.Keys.Count != _requestedAtrributesKeys.Count)
            {
                await DialogService.AlertAsync("Some proof attributes are missing");
                return;
            }

            _requestedAttributesRevealedMap.ForEach(attr => _requestedAttributesMap[attr.Key].Revealed = attr.Value);
            _requestedPredicatesMap.ForEach(pm => pm.Value.Revealed = false);

            RequestedCredentials requestedCredentials = new RequestedCredentials
            {
                RequestedAttributes = _requestedAttributesMap,
                RequestedPredicates = _requestedPredicatesMap
            };

            var context = await _agentContextProvider.GetContextAsync();
            var (msg, rec) = await _proofService.CreateProofAsync(context, _proof.Id, requestedCredentials);

            try
            {
                _ = await _messageService.SendAsync(context.Wallet, msg, rec);
            }
            catch (Exception ex)
            {
                DialogService.Alert(ex.Message);
            }

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.ProofRequestUpdated });

            await NavigationService.PopModalAsync();
        }

        private async Task LoadProofCredentials(ProofAttribute proofAttribute)
        {
            if (_proof.State != AgentFramework.Core.Models.Records.ProofState.Requested)
            {
                await DialogService.AlertAsync("Proof state should be " + AgentFramework.Core.Models.Records.ProofState.Requested.ToString());
                return;
            }
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

            await FilterCredentialRecords(proofAttribute.Name);
        }

        private async Task BuildRevealedAttributeMap(ProofAttribute proofAttribute)
        {
            if (_proof.State != AgentFramework.Core.Models.Records.ProofState.Requested)
            {
                await DialogService.AlertAsync("Proof state should be " + AgentFramework.Core.Models.Records.ProofState.Requested.ToString());
                return;
            }

            if (!_proofAttributesRevealed.ContainsKey(proofAttribute.Name))
            {
                _proofAttributesRevealed.Add(proofAttribute.Name, false);

                IsRevealed = false;
            }
            else
            {
                _proofAttributesRevealed[proofAttribute.Name] = !_proofAttributesRevealed[proofAttribute.Name];

                IsRevealed = _proofAttributesRevealed[proofAttribute.Name];
            }

            string attributeName = _requestedAtrributesKeys
                .Where(k => _requestedAttributes[k]["name"]?.ToString() == proofAttribute.Name)
                .SingleOrDefault();

            Attributes
                .ForEach(a =>
                {
                    if (a.Name == _requestedAttributes[attributeName]["name"]?.ToString())
                        a.IsRevealed = IsRevealed;
                });

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.ProofRequestAtrributeUpdated });

            _requestedAttributesRevealedMap[attributeName] = IsRevealed;
        }

        private void BuildRequestedAttributesPredicatesMap(CredentialRecord proofCredential)
        {
            IsFrameVisible = false;
            _proofAttributes[_previousProofAttribute.Keys.Single()] = false;

            RequestedAttribute requestedAttribute = new RequestedAttribute()
            {
                CredentialId = proofCredential.CredentialId,
                Revealed = true,
                Timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds()
            };
            string attributeName = _requestedAtrributesKeys
                .Where(k => _requestedAttributes[k]["name"]?.ToString() == _previousProofAttribute.Keys.Single())
                .SingleOrDefault();

            bool isPredicate = false;
            if (attributeName == null)
            {
                isPredicate = true;
                attributeName = _requestedPredicatesKeys
                    .Where(k => _requestedPredicates[k]["name"]?.ToString() == _previousProofAttribute.Keys.Single())
                    .SingleOrDefault();
            }

            Attributes
                .ForEach(a => 
                {
                    if (!isPredicate)
                    {
                        if (a.Name == _requestedAttributes[attributeName]["name"]?.ToString())
                            a.Value = proofCredential.SchemaId;
                    }
                    else
                    {
                        if (a.Name == _requestedPredicates[attributeName]["name"]?.ToString())
                            a.Value = proofCredential.SchemaId;
                    }

                });

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.ProofRequestAtrributeUpdated });

            if (!isPredicate)
            {
                if (_requestedAttributesMap.ContainsKey(attributeName))
                {
                    if (_requestedAttributesMap[attributeName]?.CredentialId != requestedAttribute.CredentialId)
                        _requestedAttributesMap[attributeName] = requestedAttribute;
                    return;
                }

                _requestedAttributesMap.Add(attributeName, requestedAttribute);
            }
            else
            {
                if (_requestedPredicatesMap.ContainsKey(attributeName))
                {
                    if (_requestedPredicatesMap[attributeName]?.CredentialId != requestedAttribute.CredentialId)
                        _requestedPredicatesMap[attributeName] = requestedAttribute;
                    return;
                }

                _requestedPredicatesMap.Add(attributeName, requestedAttribute);
            }
        }

        private async Task FilterCredentialRecords(string name)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var credentialsRecords = await _credentialService.ListAsync(context);

            IList<JObject> restrictions = new List<JObject>();
            IList<string> credentialDefinitionIds = new List<string>();

            string attributeName = _requestedAtrributesKeys
                .Where(k => _requestedAttributes[k]["name"]?.ToString() == name)
                .SingleOrDefault();

            if (attributeName == null)
            {
                attributeName = _requestedPredicatesKeys
                    .Where(k => _requestedPredicates[k]["name"]?.ToString() == name)
                    .SingleOrDefault();
                restrictions = _requestedPredicates[attributeName]["restrictions"]?.ToObject<List<JObject>>();
            }
            else
            {
                restrictions = _requestedAttributes[attributeName]["restrictions"]?.ToObject<List<JObject>>();
            }
            credentialDefinitionIds = restrictions
                    .Select(r => r["cred_def_id"]?.ToString())
                    .ToList();

            ProofCredentials = credentialsRecords
                .Where(cr => cr.State == CredentialState.Issued && credentialDefinitionIds.Contains(cr.CredentialDefinitionId))
                .ToList();
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

        public ICommand SelectProofCredentialCommand => new Command<CredentialRecord>(proofCredential =>
        {
            if (proofCredential != null) BuildRequestedAttributesPredicatesMap(proofCredential);
        });

        public ICommand RefreshCommand => new Command(_ => RefreshProofRequest());

        public ICommand ToggledCommand => new Command<ProofAttribute>(async (proofAttribute) =>
        {
            if (proofAttribute != null) await BuildRevealedAttributeMap(proofAttribute);
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

        private bool _isRevealed;
        public bool IsRevealed
        {
            get => _isRevealed;
            set => this.RaiseAndSetIfChanged(ref _isRevealed, value);
        }

        private IList<ProofAttribute> _attributes;
        public IList<ProofAttribute> Attributes
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

        private bool _refreshingProofRequest;
        public bool RefreshingProofRequest
        {
            get => _refreshingProofRequest;
            set => this.RaiseAndSetIfChanged(ref _refreshingProofRequest, value);
        }

        #endregion
    }
}
