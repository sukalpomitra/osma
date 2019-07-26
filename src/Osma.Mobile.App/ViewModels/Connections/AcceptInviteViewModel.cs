using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Exceptions;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.Connections
{
    public class AcceptInviteViewModel : ABaseViewModel
    {
        private readonly IProvisioningService _provisioningService;
        private readonly IConnectionService _connectionService;
        private readonly ICloudAgentRegistrationService _registrationService;
        private readonly IMessageService _messageService;
        private readonly ICustomAgentContextProvider _contextProvider;
        private readonly IEventAggregator _eventAggregator;
        private static readonly String GENERIC_CONNECTION_REQUEST_FAILURE_MESSAGE = "Failed to accept invite!";

        private AgentMessage _invite;

        public AcceptInviteViewModel(IUserDialogs userDialogs,
                                     INavigationService navigationService,
                                     IProvisioningService provisioningService,
                                     IConnectionService connectionService,
                                     ICloudAgentRegistrationService registrationService,
                                     IMessageService messageService,
                                     ICustomAgentContextProvider contextProvider,
                                     IEventAggregator eventAggregator)
                                     : base(nameof(AcceptInviteViewModel), userDialogs, navigationService)
        {
            _provisioningService = provisioningService;
            _connectionService = connectionService;
            _registrationService = registrationService;
            _contextProvider = contextProvider;
            _messageService = messageService;
            _contextProvider = contextProvider;
            _eventAggregator = eventAggregator;
        }

        public override Task InitializeAsync(object navigationData)
        {
            if (navigationData is ConnectionInvitationMessage invite)
            {
                InviteTitle = $"Trust {invite.Label}?";
                InviterUrl = invite.ImageUrl;
                InviteContents = $"{invite.Label} would like to establish a pairwise DID connection with you. This will allow secure communication between you and {invite.Label}.";
                _invite = invite;
            } else if (navigationData is CloudAgentRegistrationMessage registration)
            {
                InviteTitle = $"Trust {registration.Label}?";
                InviteContents = $"Would like to register {registration.Label} as your Cloud Agent?";
                _invite = registration;
            }
            return base.InitializeAsync(navigationData);
        }

        private async Task CreateConnection(IAgentContext context, ConnectionInvitationMessage invite)
        {
            var provisioningRecord = await _provisioningService.GetProvisioningAsync(context.Wallet);
            var isEndpointUriAbsent = provisioningRecord.Endpoint.Uri == null;
            var records = await _registrationService.GetAllCloudAgentAsync(context.Wallet);
            string responseEndpoint = string.Empty;
            if (records.Count > 0)
            {
                var record = _registrationService.getRandomCloudAgent(records);
                responseEndpoint = record.Endpoint.ResponseEndpoint + "/" + record.MyConsumerId;
                isEndpointUriAbsent = false;
            }
            var (msg, rec) = await _connectionService.CreateRequestAsync(context, invite, responseEndpoint);
            var rsp = await _messageService.SendAsync(context.Wallet, msg, rec, invite.RecipientKeys.First(), isEndpointUriAbsent);
            if (isEndpointUriAbsent)
            {
                await _connectionService.ProcessResponseAsync(context, rsp.GetMessage<ConnectionResponseMessage>(), rec);
            }
        }

        private async Task RegisterCloudAgent(IAgentContext context, CloudAgentRegistrationMessage registration)
        {
            var records = await _registrationService.GetAllCloudAgentAsync(context.Wallet);
            if (records.FindAll(x => x.Label.Equals(registration.Label)).Count != 0)
            {
                throw new AgentFrameworkException(ErrorCode.CloudAgentAlreadyRegistered, $"{registration.Label} already registered!");
            }
            await _registrationService.RegisterCloudAgentAsync(context, registration);
        }

        #region Bindable Commands
        public ICommand AcceptInviteCommand => new Command(async () =>
        {
            var loadingDialog = DialogService.Loading("Processing");

            var context = await _contextProvider.GetContextAsync();

            if (context == null || _invite == null)
            {
                loadingDialog.Hide();
                DialogService.Alert("Failed to decode invite!");
                return;
            }

            String errorMessage = String.Empty;
            try
            {
                if (_invite is ConnectionInvitationMessage)
                {
                    await CreateConnection(context, (ConnectionInvitationMessage)_invite);
                } else if (_invite is CloudAgentRegistrationMessage)
                {
                    await RegisterCloudAgent(context, (CloudAgentRegistrationMessage)_invite);
                    DialogService.Alert("Cloud Agent registered successfully!");
                }
            }
            catch (AgentFrameworkException agentFrameworkException)
            {
                errorMessage = agentFrameworkException.Message;
            }
            catch (Exception) //TODO more granular error protection
            {
                errorMessage = GENERIC_CONNECTION_REQUEST_FAILURE_MESSAGE;
            }

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.ConnectionsUpdated });

            if (loadingDialog.IsShowing)
                loadingDialog.Hide();

            if (!String.IsNullOrEmpty(errorMessage))
                DialogService.Alert(errorMessage);

            await NavigationService.PopModalAsync();
        });

        public ICommand RejectInviteCommand => new Command(async () => await NavigationService.PopModalAsync());

        #endregion

        #region Bindable Properties
        private string _inviteTitle;
        public string InviteTitle
        {
            get => _inviteTitle;
            set => this.RaiseAndSetIfChanged(ref _inviteTitle, value);
        }

        private string _inviteContents = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua";
        public string InviteContents
        {
            get => _inviteContents;
            set => this.RaiseAndSetIfChanged(ref _inviteContents, value);
        }

        private string _inviterUrl;
        public string InviterUrl
        {
            get => _inviterUrl;
            set => this.RaiseAndSetIfChanged(ref _inviterUrl, value);
        }
        #endregion
    }
}
