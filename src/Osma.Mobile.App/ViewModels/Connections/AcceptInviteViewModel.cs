using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;
using System.Net.Http;
using Plugin.Fingerprint;
using System.Reactive.Linq;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.CloudRegistrationMessage;
using Hyperledger.Aries;

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
        private readonly INavigationService _navigationService;
        private readonly IUserDialogs _userDialogs;
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
            _eventAggregator = eventAggregator;
            _navigationService = navigationService;
            _userDialogs = userDialogs;
            MessagingCenter.Subscribe<PassCodeViewModel, CloudAgentRegistrationMessage>(this, ApplicationEventType.PassCodeAuthorisedCloudAgent.ToString(), async (p, o) =>
            {
                await RegisterCloudAgentAfterAuth(await _contextProvider.GetContextAsync(), o, true);
            });

            MessagingCenter.Subscribe<PassCodeViewModel, ConnectionInvitationMessage>(this, ApplicationEventType.PassCodeAuthorised.ToString(), async (p, o) =>
            {
                await CreateConnectionAfterAuth(await _contextProvider.GetContextAsync(), o, true);
            });
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
            if (await isAuthenticatedAsync(null, invite))
            {
                await CreateConnectionAfterAuth(context, invite, false);
            }
        }

        private async Task CreateConnectionAfterAuth(IAgentContext context, ConnectionInvitationMessage invite, bool showLoader)
        {
            MessagingCenter.Unsubscribe<PassCodeViewModel, ConnectionInvitationMessage>(this, ApplicationEventType.PassCodeAuthorised.ToString());
            MessagingCenter.Unsubscribe<PassCodeViewModel>(this, ApplicationEventType.PassCodeAuthorised.ToString());
            IProgressDialog loading = null;
            if (showLoader)
            {
                loading = DialogService.Loading("Processing");
            }
            var provisioningRecord = await _provisioningService.GetProvisioningAsync(context.Wallet);
            var records = await _registrationService.GetAllCloudAgentAsync(context.Wallet);
            string responseEndpoint = string.Empty;
            if (records.Count > 0)
            {
                var record = _registrationService.getRandomCloudAgent(records);
                responseEndpoint = record.Endpoint.ResponseEndpoint + "/" + record.MyConsumerId;
            }
            bool newSsoConnection = true;
            if (invite.Sso)
            {
                var connections = await _connectionService.ListAsync(context);
                if (connections.FindAll(con => invite.Label.Equals(con.Alias.Name)).Count != 0)
                {
                    newSsoConnection = false;
                    var con = connections.Where(conn => invite.Label.Equals(conn.Alias.Name)).First();
                    var endpoint = con.Endpoint.Uri.Replace("response", "trigger/") + con.MyDid + "/" + invite.InvitationKey;
                    HttpClient httpClient = new HttpClient();
                    await httpClient.GetAsync(new System.Uri(endpoint));
                }
            }
            if (newSsoConnection)
            {
                var (msg, rec) = await _connectionService.CreateRequestAsync(context, invite, responseEndpoint);
                await _messageService.SendAsync(context.Wallet, msg, invite.RecipientKeys.First(), responseEndpoint);
            }
            if (showLoader)
            {
                loading.Hide();
            }
        }

        private async Task RegisterCloudAgent(IAgentContext context, CloudAgentRegistrationMessage registration)
        {
            if (await isAuthenticatedAsync(registration, null))
            {
                await RegisterCloudAgentAfterAuth(context, registration, false);
            }
        }


        private async Task RegisterCloudAgentAfterAuth(IAgentContext context, CloudAgentRegistrationMessage registration, bool showLoader)
        {
            IProgressDialog loading = null;
            if (showLoader)
            {
                loading = DialogService.Loading("Processing");
            }
            var records = await _registrationService.GetAllCloudAgentAsync(context.Wallet);
            bool progress = true;
            if (records.FindAll(x => x.Label.Equals(registration.Label)).Count != 0)
            {
                string error = $"{registration.Label} already registered!";
                if (!showLoader)
                {
                    throw new AriesFrameworkException(ErrorCode.CloudAgentAlreadyRegistered, error);
                }
                else
                {
                    progress = false;
                    loading.Hide();
                    DialogService.Alert(error);
                }
            }
            if (progress)
            {
                await _registrationService.RegisterCloudAgentAsync(context, registration);
                if (showLoader)
                {
                    loading.Hide();
                }
                DialogService.Alert("Cloud Agent registered successfully!");
            }
        }

        private async Task<bool> isAuthenticatedAsync(CloudAgentRegistrationMessage registration, ConnectionInvitationMessage invite)
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
            } else
            {
                authenticated = false;
                var vm = new PassCodeViewModel(_userDialogs, _navigationService, _contextProvider, _provisioningService);
                if (invite == null)
                {
                    vm.Event = ApplicationEventType.PassCodeAuthorisedCloudAgent;
                    vm.Registration = registration;
                } else
                {
                    vm.Event = ApplicationEventType.PassCodeAuthorised;
                    vm.Invite = invite;
                }
                await NavigationService.NavigateToPopupAsync<PassCodeViewModel>(true, vm);
            }
            return authenticated;
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
                }
            }
            catch (AriesFrameworkException ariesFrameworkException)
            {
                errorMessage = ariesFrameworkException.Message;
            }
            catch (Exception ex) //TODO more granular error protection
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
