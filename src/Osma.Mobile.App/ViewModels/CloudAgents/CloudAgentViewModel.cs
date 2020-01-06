using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;
using Plugin.Fingerprint;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;

namespace Osma.Mobile.App.ViewModels.CloudAgents
{
    public class CloudAgentViewModel : ABaseViewModel
    {
        private readonly CloudAgentRegistrationRecord _record;

        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ICloudAgentRegistrationService _registrationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IProvisioningService _provisioningService;
        private readonly INavigationService _navigationService;
        private readonly IUserDialogs _userDialogs;

        public CloudAgentViewModel(IUserDialogs userDialogs,
                                   INavigationService navigationService,
                                   ICustomAgentContextProvider agentContextProvider,
                                   IEventAggregator eventAggregator,
                                   ICloudAgentRegistrationService registrationService,
                                   IProvisioningService provisioningService,
                                   CloudAgentRegistrationRecord record) :
                                   base(nameof(CloudAgentViewModel),
                                       userDialogs,
                                       navigationService)
        {
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _registrationService = registrationService;
            _record = record;
            _userDialogs = userDialogs;
            _navigationService = navigationService;
            _provisioningService = provisioningService;
            CloudAgentLabel = _record.Label;
            TheirVK = _record.TheirVk;
            CloudAgentName = _record.GetType().Name;
            CloudAgentEndPoint = _record.Endpoint;

            MessagingCenter.Subscribe<PassCodeViewModel>(this, ApplicationEventType.PassCodeAuthorisedDeleteCloudAgent.ToString(), async (p) => {
                await DeleteCloudAgent();
            });
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
            if (await isAuthenticatedAsync(ApplicationEventType.PassCodeAuthorisedDeleteCloudAgent))
            {
                await DeleteCloudAgent();
            }
        });

        #endregion

        private async Task DeleteCloudAgent()
        {
            var context = await _agentContextProvider.GetContextAsync();
            await _registrationService.removeCloudAgentAsync(context.Wallet, _record.Id);

            _eventAggregator.Publish(new ApplicationEvent() { Type = ApplicationEventType.CloudAgentsUpdated });

            await NavigationService.NavigateBackAsync();
        }

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
                await NavigationService.NavigateToPopupAsync<PassCodeViewModel>(true, vm);
            }
            return authenticated;
        }

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
