using System;
using System.Windows.Input;
using Acr.UserDialogs;
using Autofac;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Features.CloudRegistrationMessage;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels
{
    public class PassCodeViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;
        private readonly IProvisioningService _provisioningService;

        public PassCodeViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider,
                                 IProvisioningService provisioningService
                                 ) : base(
                                 nameof(PassCodeViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
            _provisioningService = provisioningService;
            _passCode = "";
        }


        #region Bindable Property
        private string _passCode;
        public string PassCode
        {
            get => _passCode;
            set => this.RaiseAndSetIfChanged(ref _passCode, value);
        }

        private ApplicationEventType _event;
        public ApplicationEventType Event
        {
            get => _event;
            set => this.RaiseAndSetIfChanged(ref _event, value);
        }

        private ConnectionInvitationMessage _invite;
        public ConnectionInvitationMessage Invite
        {
            get => _invite;
            set => this.RaiseAndSetIfChanged(ref _invite, value);
        }

        private CloudAgentRegistrationMessage _registration;
        public CloudAgentRegistrationMessage Registration
        {
            get => _registration;
            set => this.RaiseAndSetIfChanged(ref _registration, value);
        }

        private CredentialRecord _credential;
        public CredentialRecord Credential
        {
            get => _credential;
            set => this.RaiseAndSetIfChanged(ref _credential, value);
        }

        #endregion

        #region Bindable Command    
        public ICommand Validate => new Command(async () =>
        {
            var context = await _agentContextProvider.GetContextAsync();
            var provisioningRecord = await _provisioningService.GetProvisioningAsync(context.Wallet);
            if (provisioningRecord.PassCode.Equals(PassCode))
            {
                await _navigationService.CloseAllPopupsAsync();
                switch (Event)
                {
                    case ApplicationEventType.PassCodeAuthorisedCloudAgent:
                        MessagingCenter.Send(this, Event.ToString(), Registration);
                        break;
                    case ApplicationEventType.PassCodeAuthorised:
                        MessagingCenter.Send(this, Event.ToString(), Invite);
                        break;
                    case ApplicationEventType.PassCodeAuthorisedDeleteConnection:
                    case ApplicationEventType.PassCodeAuthorisedSSO:
                    case ApplicationEventType.PassCodeAuthorisedDeleteCloudAgent:
                    case ApplicationEventType.PassCodeAuthorisedCredentialReject:
                    case ApplicationEventType.PassCodeAuthorisedProofAccept:
                    case ApplicationEventType.PassCodeAuthorisedProofReject:
                        MessagingCenter.Send(this, Event.ToString());
                        break;
                    case ApplicationEventType.PassCodeAuthorisedCredentialAccept:
                        MessagingCenter.Send(this, Event.ToString(), Credential);
                        break;
                    default:
                        break;
                }
            } else
            {
                DialogService.Alert("Wrong Passcode.");
            }
        }, () => false);

        public ICommand Cancel => new Command(async () =>
        {
            DialogService.Alert("Unathorised. Canceling Activity.");
            await _navigationService.CloseAllPopupsAsync();
        });

        #endregion
    }
}
