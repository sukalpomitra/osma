using System;
using System.Collections.Generic;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using Xamarin.Forms;
using ReactiveUI;
using System.Threading.Tasks;
using Autofac;
using ZXing.Net.Mobile.Forms;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.ViewModels.Connections;
using Osma.Mobile.App.Extensions;
using System.Linq;
using Osma.Mobile.App.Events;
using System.Reactive.Linq;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Utils;
using Hyperledger.Aries.Features.CloudRegistrationMessage;
using Hyperledger.Aries.Features.DidExchange;
using System.Collections;

namespace Osma.Mobile.App.ViewModels.CloudAgents
{
    public class CloudAgentsViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ICloudAgentRegistrationService _registrationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILifetimeScope _scope;
        private readonly IMessageService _messageService;
        private bool isRefreshStarted = false;

        private static Queue queue = new Queue();

        public CloudAgentsViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider,
                                 ICloudAgentRegistrationService registrationService,
                                 IMessageService messageService,
                                 IEventAggregator eventAggregator,
                                 ILifetimeScope scope) : base(
                                 nameof(CloudAgentsViewModel),
                                 userDialogs, 
                                 navigationService)
        {
            _agentContextProvider = agentContextProvider;
            _registrationService = registrationService;
            _scope = scope;
            _eventAggregator = eventAggregator;
            _messageService = messageService;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshCloudAgents();
            if (!isRefreshStarted) _ = BackgroundRefreshCloudAgents();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                            .Where(_ => _.Type == ApplicationEventType.CloudAgentsUpdated)
                            .Subscribe(async _ => await RefreshCloudAgents());

            await base.InitializeAsync(navigationData);
        }

        public async Task BackgroundRefreshCloudAgents()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var agent = await _agentContextProvider.GetAgentAsync();
            isRefreshStarted = true;
            for (long i = 0; i <= long.MaxValue; i++)
            {
                await Task.Delay(5000);
                while (queue.Count > 0)
                {
                    await agent.ProcessAsync(context, (MessageContext)queue.Dequeue());
                }
                await RefreshCloudAgents();
            }
        }

        public async Task RefreshCloudAgents()
        {
            RefreshingCloudAgents = true;
            
            var context = await _agentContextProvider.GetContextAsync();
            var agent = await _agentContextProvider.GetAgentAsync();
            var records = await _registrationService.GetAllCloudAgentAsync(context.Wallet);
            var cloudAgentVms = records
                .Select(r => _scope.Resolve<CloudAgentViewModel>(new NamedParameter("record", r)))
                .ToList();

            CloudAgentsGrouped.Clear();
            CloudAgentsGrouped.InsertRange(cloudAgentVms);
            HasCloudAgents = cloudAgentVms.Any();

            if (CloudAgentsGrouped.Count > 0)
            {
                try
                {
                    var messages = await _messageService.ConsumeAsync(context.Wallet);
                    foreach (var message in messages)
                    {
                        queue.Enqueue(message);
                    }
                }
                catch (Exception e)
                {
                    // ignored
                    // DialogService.Alert(ex.Message);
                }
            }
            
            RefreshingCloudAgents = false;
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

            await NavigationService.NavigateToAsync(scannerPage, NavigationType.Modal);
        }

        public async Task SelectCloudAgent(CloudAgentViewModel cloudAgent) => await NavigationService.NavigateToAsync(cloudAgent);

        #region Bindable Command

        public ICommand RefreshCommand => new Command(async () => await RefreshCloudAgents());

        public ICommand ScanInviteCommand => new Command(async () => await ScanInvite());

        public ICommand SelectCloudAgentCommand => new Command<CloudAgentViewModel>(async (cloudAgent) =>
        {
            if (cloudAgent != null) await SelectCloudAgent(cloudAgent);
        });

        #endregion

        #region Bindable Properties

        private RangeEnabledObservableCollection<CloudAgentViewModel> _cloudAgents = new RangeEnabledObservableCollection<CloudAgentViewModel>();
        public RangeEnabledObservableCollection<CloudAgentViewModel> CloudAgentsGrouped
        {
            get => _cloudAgents;
            set => this.RaiseAndSetIfChanged(ref _cloudAgents, value);
        }

        private bool _refreshingCloudAgents;
        public bool RefreshingCloudAgents
        {
            get => _refreshingCloudAgents;
            set => this.RaiseAndSetIfChanged(ref _refreshingCloudAgents, value);
        }

        private bool _hasCloudAgents;
        public bool HasCloudAgents
        {
            get => _hasCloudAgents;
            set => this.RaiseAndSetIfChanged(ref _hasCloudAgents, value);
        }

        #endregion
    }
}
