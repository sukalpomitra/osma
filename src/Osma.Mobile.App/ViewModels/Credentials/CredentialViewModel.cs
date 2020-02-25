using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;
using System.Threading.Tasks;
using Osma.Mobile.App.Events;
using Plugin.Fingerprint;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;

namespace Osma.Mobile.App.ViewModels.Credentials
{
    public class CredentialViewModel : ABaseViewModel
    {
        private readonly CredentialRecord _credential;

        private readonly ICredentialService _credentialService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IMessageService _messageService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IProvisioningService _provisioningService;
        private readonly INavigationService _navigationService;
        private readonly IUserDialogs _userDialogs;

        public CredentialViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            ICredentialService credentialService,
            ICustomAgentContextProvider agentContextProvider,
            IMessageService messageService,
            IEventAggregator eventAggregator,
            IProvisioningService provisioningService,
            CredentialRecord credential
        ) : base(
            nameof(CredentialViewModel),
            userDialogs,
            navigationService
        )
        {
            _credential = credential;
            _credentialService = credentialService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;
            _eventAggregator = eventAggregator;
            _userDialogs = userDialogs;
            _navigationService = navigationService;
            _provisioningService = provisioningService;
            CredentialName = (credential.SchemaId.Split(':')[2]).Replace(" schema", "") + " - " + (credential.SchemaId.Split(':')[3]);
            CredentialSubtitle = credential.State.ToString();

            if (credential.State == CredentialState.Issued && credential.CredentialAttributesValues != null)
            {
                Attributes = credential.CredentialAttributesValues
                    .Select(p => 
                        new CredentialAttribute()
                        {
                            Name = p.Name,
                            Value = p.Value?.ToString(),
                            Type = "Text"
                        })
                    .ToList();
            }

            _isNew = IsCredentialNew(_credential);
            MessagingCenter.Subscribe<PassCodeViewModel, CredentialRecord>(this, ApplicationEventType.PassCodeAuthorisedCredentialAccept.ToString(), async (p, o) =>
            {
                await AcceptCredentialOffer(o);
            });

            MessagingCenter.Subscribe<PassCodeViewModel>(this, ApplicationEventType.PassCodeAuthorisedCredentialReject.ToString(), async (p) =>
            {
                await RejectCredentialOffer();
            });
        }

        private bool IsCredentialNew(CredentialRecord credential)
        {
            // TODO OS-200, Currently a Placeholder for a mix of new and not new cells
            Random random = new Random();
            return random.Next(0, 2) == 1;
        }

        private async Task AcceptCredentialOffer(CredentialRecord credentialRecord)
        {
            MessagingCenter.Unsubscribe<PassCodeViewModel, CredentialRecord>(this, ApplicationEventType.PassCodeAuthorisedCredentialAccept.ToString());
            MessagingCenter.Unsubscribe<PassCodeViewModel>(this, ApplicationEventType.PassCodeAuthorisedCredentialAccept.ToString());
            if (credentialRecord.State != CredentialState.Offered)
            {
                await DialogService.AlertAsync("Credential state should be " + CredentialState.Offered.ToString());
                await NavigationService.PopModalAsync();
                return;
            }

            var context = await _agentContextProvider.GetContextAsync();
            var (msg, rec) = await _credentialService.CreateRequestAsync(context, credentialRecord.Id);

            await _messageService.SendAsync(context.Wallet, msg, rec.TheirVk ?? rec.GetTag("InvitationKey") ?? throw new InvalidOperationException("Cannot locate a recipient key"), rec.Endpoint.Uri,
                rec.Endpoint?.Verkey == null ? null : rec.Endpoint.Verkey, rec.MyVk);

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });

            await NavigationService.PopModalAsync();
        }

        private async Task RejectCredentialOffer()
        {
            if (_credential.State != CredentialState.Offered)
            {
                await DialogService.AlertAsync("Credential state should be " + CredentialState.Offered.ToString());
                await NavigationService.PopModalAsync();
                return;
            }

            var context = await _agentContextProvider.GetContextAsync();
            await _credentialService.RejectOfferAsync(context, _credential.Id);

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CredentialUpdated });

            await NavigationService.PopModalAsync();
        }

        #region Bindable Command

        public ICommand NavigateBackCommand => new Command(async () => await NavigationService.PopModalAsync());

        public ICommand AcceptCredentialOfferCommand => new Command(async () => {
            if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedCredentialAccept))
            {
                await AcceptCredentialOffer(_credential);
            }
        });

        public ICommand RejectCredentialOfferCommand => new Command(async () => {
            if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedCredentialReject))
            {
                await RejectCredentialOffer();
            }
        });

        #endregion

        private async Task<bool> isAuthenticatedAsync(ApplicationEventType eventType)
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
            else
            {
                authenticated = false;
                var vm = new PassCodeViewModel(_userDialogs, _navigationService, _agentContextProvider, _provisioningService);
                vm.Event = eventType;
                vm.Credential = _credential;
                await NavigationService.NavigateToPopupAsync<PassCodeViewModel>(true, vm);
            }
            return authenticated;
        }

        #region Bindable Properties

        private string _credentialName;
        public string CredentialName
        {
            get => _credentialName;
            set => this.RaiseAndSetIfChanged(ref _credentialName, value);
        }

        private string _credentialType;
        public string CredentialType
        {
            get => _credentialType;
            set => this.RaiseAndSetIfChanged(ref _credentialType, value);
        }

        private string _credentialImageUrl;
        public string CredentialImageUrl
        {
            get => _credentialImageUrl;
            set => this.RaiseAndSetIfChanged(ref _credentialImageUrl, value);
        }

        private string _credentialSubtitle;
        public string CredentialSubtitle
        {
            get => _credentialSubtitle;
            set => this.RaiseAndSetIfChanged(ref _credentialSubtitle, value);
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set => this.RaiseAndSetIfChanged(ref _isNew, value);
        }

        private string _qRImageUrl;
        public string QRImageUrl
        {
            get => _qRImageUrl;
            set => this.RaiseAndSetIfChanged(ref _qRImageUrl, value);
        }

        private IEnumerable<CredentialAttribute> _attributes;
        public IEnumerable<CredentialAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        #endregion
    }
}
