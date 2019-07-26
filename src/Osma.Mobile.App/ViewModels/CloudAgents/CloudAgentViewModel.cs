using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models;
using AgentFramework.Core.Models.Records;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.CloudAgents
{
    public class CloudAgentViewModel : ABaseViewModel
    {
        private readonly CloudAgentRegistrationRecord _record;

        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ICloudAgentRegistrationService _registrationService;
        private readonly IEventAggregator _eventAggregator;

        public CloudAgentViewModel(IUserDialogs userDialogs,
                                   INavigationService navigationService,
                                   ICustomAgentContextProvider agentContextProvider,
                                   IEventAggregator eventAggregator,
                                   ICloudAgentRegistrationService registrationService,
                                   CloudAgentRegistrationRecord record) :
                                   base(nameof(CloudAgentViewModel),
                                       userDialogs,
                                       navigationService)
        {
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _registrationService = registrationService;

            _record = record;
            CloudAgentLabel = _record.Label;
            TheirVK = _record.TheirVk;
            CloudAgentName = _record.GetType().Name;
            CloudAgentEndPoint = _record.Endpoint;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await base.InitializeAsync(navigationData);
        }

        #region Bindable Command
        public ICommand NavigateBackCommand => new Command(async () =>
        {
            await NavigationService.NavigateBackAsync();
        });

        public ICommand DeleteCloudAgentCommand => new Command(async () =>
        {
            var dialog = DialogService.Loading("Deleting ...");

            var context = await _agentContextProvider.GetContextAsync();
            await _registrationService.removeCloudAgentAsync(context.Wallet, _record.Id);

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CloudAgentsUpdated });

            if (dialog.IsShowing)
            {
                dialog.Hide();
                dialog.Dispose();
            }

            await NavigationService.NavigateBackAsync();
        });

        #endregion

        #region Bindable Properties
        private string _cloudAgentName;
        public string CloudAgentName
        {
            get => _cloudAgentName;
            set => this.RaiseAndSetIfChanged(ref _cloudAgentName, value);
        }

        private string _cloudAgentLabel;
        public string CloudAgentLabel
        {
            get => _cloudAgentLabel;
            set => this.RaiseAndSetIfChanged(ref _cloudAgentLabel, value);
        }

        private CloudAgentEndpoint _cloudAgentEndPoint;
        public CloudAgentEndpoint CloudAgentEndPoint
        {
            get => _cloudAgentEndPoint;
            set => this.RaiseAndSetIfChanged(ref _cloudAgentEndPoint, value);
        }

        public string CloudAgentEndPointString
        { get => CloudAgentEndPoint.ToString(); }

        private string _theirVk;
        public string TheirVK
        {
            get => _theirVk;
            set => this.RaiseAndSetIfChanged(ref _theirVk, value);
        }
        #endregion
    }
}
