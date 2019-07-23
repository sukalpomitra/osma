using System;
using System.Collections.Generic;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using Xamarin.Forms;
using ReactiveUI;
using AgentFramework.Core.Models.Records;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using Autofac;
using ZXing.Net.Mobile.Forms;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Utils;
using AgentFramework.Core.Messages.Connections;
using Osma.Mobile.App.ViewModels.Connections;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using Osma.Mobile.App.ViewModels.Account;

namespace Osma.Mobile.App.ViewModels.CloudAgents
{
    public class CloudAgentsViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ICloudAgentRegistrationService _registrationService;
        private readonly ILifetimeScope _scope;
        private readonly IMessageService _messageService;

        public CloudAgentsViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider,
                                 ICloudAgentRegistrationService registrationService,
                                 IMessageService messageService,
                                 ILifetimeScope scope) : base(
                                 nameof(CloudAgentsViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _agentContextProvider = agentContextProvider;
            _registrationService = registrationService;
            _scope = scope;
            _messageService = messageService;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshCloudAgents();
            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshCloudAgents()
        {
            RefreshingCloudAgents = true;
            
            var context = await _agentContextProvider.GetContextAsync();
            var agent = await _agentContextProvider.GetAgentAsync();

            CloudAgentsGrouped = await _registrationService.GetAllCloudAgentAsync(context.Wallet);

            if (CloudAgentsGrouped.Count > 0)
            {
                var messages = await _messageService.ConsumeAsync(context.Wallet);
                try
                {
                    foreach (var message in messages)
                    {
                        await agent.ProcessAsync(context, message);
                    }
                }
                catch (Exception) { }
            }
            
            RefreshingCloudAgents = false;
        }

        public async Task DeleteCloudAgent()
        {

        }

        public async Task ScanInvite()
        {
            var expectedFormat = ZXing.BarcodeFormat.QR_CODE;
            var opts = new ZXing.Mobile.MobileBarcodeScanningOptions { PossibleFormats = new List<ZXing.BarcodeFormat> { expectedFormat } };

            var scannerPage = new ZXingScannerPage(opts);
            scannerPage.OnScanResult += (result) => {
                scannerPage.IsScanning = false;

                AgentMessage invitation;
                var messageType = result.Text.Contains("c_a_r=") ? MessageTypes.CloudAgentRegistration : MessageTypes.ConnectionInvitation;

                try
                {
                    switch (messageType)
                    {
                        case MessageTypes.CloudAgentRegistration:
                            invitation = MessageUtils.DecodeMessageFromUrlFormat<CloudAgentRegistrationMessage>(result.Text);
                            break;
                        case MessageTypes.ConnectionInvitation:
                            invitation = MessageUtils.DecodeMessageFromUrlFormat<ConnectionInvitationMessage>(result.Text);
                            break;
                        default:
                            invitation = null;
                            break;
                    }
                    if (invitation == null)
                    {
                        throw new InvalidOperationException();
                    }
                }
                catch (Exception)
                {
                    DialogService.Alert("Invalid invitation!");
                    Device.BeginInvokeOnMainThread(async () => await NavigationService.PopModalAsync());
                    return;
                }

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await NavigationService.PopModalAsync();
                    await NavigationService.NavigateToAsync<AcceptInviteViewModel>(invitation, NavigationType.Modal);
                });
            };

            await NavigationService.NavigateToAsync((Page)scannerPage, NavigationType.Modal);
        }

        #region Bindable Command
        public ICommand RefreshCommand => new Command(async () => await RefreshCloudAgents());

        public ICommand DeleteCommand => new Command(async () => await DeleteCloudAgent());

        public ICommand ScanInviteCommand => new Command(async () => await ScanInvite());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());

        #endregion

        #region Bindable Properties
        private List<CloudAgentRegistrationRecord> _cloudAgentsGrouped;
        public List<CloudAgentRegistrationRecord> CloudAgentsGrouped
        {
            get => _cloudAgentsGrouped;
            set => this.RaiseAndSetIfChanged(ref _cloudAgentsGrouped, value);
        }

        private bool _refreshingCloudAgents;
        public bool RefreshingCloudAgents
        {
            get => _refreshingCloudAgents;
            set => this.RaiseAndSetIfChanged(ref _refreshingCloudAgents, value);
        }

        #endregion
    }
}
