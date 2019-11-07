using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Messages;
using AgentFramework.Core.Messages.Connections;
using AgentFramework.Core.Messages.Proofs;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Utils;
using Autofac;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.CloudAgents;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using ReactiveUI;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;

namespace Osma.Mobile.App.ViewModels.Connections
{
    public class ConnectionsViewModel : ABaseViewModel
    {
        private readonly IConnectionService _connectionService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILifetimeScope _scope;
        private bool isRefreshStarted = false;
        private readonly IProofService _proofService;

        public ConnectionsViewModel(IUserDialogs userDialogs,
                                    INavigationService navigationService,
                                    IConnectionService connectionService,
                                    ICustomAgentContextProvider agentContextProvider,
                                    IEventAggregator eventAggregator,
                                    IProofService proofService,
                                    ILifetimeScope scope) :
                                    base(
                                        nameof(ConnectionsViewModel), 
                                        userDialogs, 
                                        navigationService)
        {
            _connectionService = connectionService;
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _scope = scope;
            _proofService = proofService;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshConnections();
            if (!isRefreshStarted) _ = BackgroundRefreshConnections();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                            .Where(_ => _.Type == ApplicationEventType.ConnectionsUpdated)
                            .Subscribe(async _ => await RefreshConnections());

            await base.InitializeAsync(navigationData);
        }

        public async Task BackgroundRefreshConnections()
        {
            isRefreshStarted = true;
            for (long i = 0; i <= long.MaxValue; i++)
            {
                await Task.Delay(5000);
                await RefreshConnections();
            }
        }
        
        public async Task RefreshConnections()
        {
            RefreshingConnections = true;

            var context = await _agentContextProvider.GetContextAsync();
            var records = await _connectionService.ListAsync(context);

            IList<ConnectionViewModel> connectionVms = new List<ConnectionViewModel>();
            foreach (var record in records)
            {
                var connection = _scope.Resolve<ConnectionViewModel>(new NamedParameter("record", record));
                connectionVms.Add(connection);
            }

            //TODO need to compare with the currently displayed connections rather than disposing all of them
            Connections.Clear();
            Connections.InsertRange(connectionVms);
            HasConnections = connectionVms.Any();

            RefreshingConnections = false;
        }

        public async Task ScanInvite()
        {
            var expectedFormat = ZXing.BarcodeFormat.QR_CODE;
            var opts = new ZXing.Mobile.MobileBarcodeScanningOptions{ PossibleFormats = new List<ZXing.BarcodeFormat> { expectedFormat }};
            var context = await _agentContextProvider.GetContextAsync();

            var scannerPage = new ZXingScannerPage(opts);
            scannerPage.OnScanResult += (result) => {
                scannerPage.IsScanning = false;
                var url = result.Text;
                if (!result.Text.Contains("?"))
                {
                    url = UrlLengthen(result.Text);
                }

                AgentMessage invitation;
                var messageType = url.Contains("c_a_r=") ?
                MessageTypes.CloudAgentRegistration : url.Contains("m=") ?
                MessageTypes.ProofRequest
                : MessageTypes.ConnectionInvitation;
                try
                {
                    switch (messageType)
                    {
                        case MessageTypes.CloudAgentRegistration:
                            invitation = MessageUtils.DecodeMessageFromUrlFormat<CloudAgentRegistrationMessage>(url);
                            break;
                        case MessageTypes.ConnectionInvitation:
                            invitation = MessageUtils.DecodeMessageFromUrlFormat<ConnectionInvitationMessage>(url);
                            break;
                        case MessageTypes.ProofRequest:
                            invitation = MessageUtils.DecodeMessageFromUrlFormat<ProofRequestMessage>(url);
                            ProofRequestMessage proofRequest = (ProofRequestMessage)invitation;
                            var connection = new ConnectionRecord {
                                TheirVk = proofRequest.ServiceDecorator.RecipientKeys[0],
                                Sso = false
                            };
                            _proofService.ProcessProofRequestAsync(context, proofRequest, connection, true);
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
                catch (Exception ex)
                {
                    DialogService.Alert("Invalid invitation!");
                    Device.BeginInvokeOnMainThread(async () => await NavigationService.PopModalAsync());
                    return;
                }

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await NavigationService.PopModalAsync();
                    if (messageType != MessageTypes.ProofRequest)
                    {
                        await NavigationService.NavigateToAsync<AcceptInviteViewModel>(invitation, NavigationType.Modal);
                    }
                });
            };

            await NavigationService.NavigateToAsync((Page)scannerPage, NavigationType.Modal);
        }

        private string UrlLengthen(string url)
        {
            string newurl = url;

            bool redirecting = true;

            while (redirecting)
            {

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(newurl);
                    request.AllowAutoRedirect = false;
                    request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3 (.NET CLR 4.0.20506)";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if ((int)response.StatusCode == 301 || (int)response.StatusCode == 302)
                    {
                        string uriString = response.Headers["Location"];
                        newurl = uriString;
                        if (newurl.Contains("c_a_r=") ||
                            newurl.Contains("c_i=") || newurl.Contains("m="))
                        {
                            redirecting = false;
                        }
                    }
                    else
                    {
                        redirecting = false;
                    }
                }
                catch (Exception ex)
                {
                    ex.Data.Add("url", newurl);
                    redirecting = false;
                }
            }
            return newurl;
        }

        public async Task SelectConnection(ConnectionViewModel connection) => await NavigationService.NavigateToAsync(connection);

        #region Bindable Command
        public ICommand RefreshCommand => new Command(async () => await RefreshConnections());

        public ICommand ScanInviteCommand => new Command(async () => await ScanInvite());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand CloudAgentsCommand => new Command(async () => await NavigationService.NavigateToAsync<CloudAgentsViewModel>());

        public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());

        public ICommand SelectConnectionCommand => new Command<ConnectionViewModel>(async (connection) =>
        {
            if (connection != null)
                await SelectConnection(connection);
        });
        #endregion

        #region Bindable Properties
        private RangeEnabledObservableCollection<ConnectionViewModel> _connections = new RangeEnabledObservableCollection<ConnectionViewModel>();
        public RangeEnabledObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections;
            set => this.RaiseAndSetIfChanged(ref _connections, value);
        }

        private bool _hasConnections;
        public bool HasConnections
        {
            get => _hasConnections;
            set => this.RaiseAndSetIfChanged(ref _hasConnections, value);
        }

        private bool _refreshingConnections;
        public bool RefreshingConnections
        {
            get => _refreshingConnections;
            set => this.RaiseAndSetIfChanged(ref _refreshingConnections, value);
        }
        #endregion
    }
}
