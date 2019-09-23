using System.Threading.Tasks;
using Autofac;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.CloudAgents;
using Osma.Mobile.App.ViewModels.Connections;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using Osma.Mobile.App.ViewModels.Credentials;
using Osma.Mobile.App.ViewModels.ProofRequests;
using Osma.Mobile.App.Views;
using Osma.Mobile.App.Views.Account;
using Osma.Mobile.App.Views.CloudAgents;
using Osma.Mobile.App.Views.Connections;
using Osma.Mobile.App.Views.CreateInvitation;
using Osma.Mobile.App.Views.Credentials;
using Osma.Mobile.App.Views.ProofRequests;
using Osma.Mobile.App.Views.Security;
using Osma.Mobile.App.Views.UserRegistration;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using MainPage = Osma.Mobile.App.Views.MainPage;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Osma.Mobile.App
{
    public partial class App : Application
    {
        public new static App Current => Application.Current as App;
        public Palette Colors;

        private readonly INavigationService _navigationService;
        private readonly ICustomAgentContextProvider _contextProvider;

        public App(IContainer container)
        {
            InitializeComponent();
            XF.Material.Forms.Material.Init(this);

            Colors.Init();
            _navigationService = container.Resolve<INavigationService>();
            _contextProvider = container.Resolve<ICustomAgentContextProvider>();

            InitializeTask = Initialize();
        }

        Task InitializeTask;
        private async Task Initialize()
        {
            _navigationService.AddPageViewModelBinding<MainViewModel, MainPage>();
            _navigationService.AddPageViewModelBinding<ConnectionsViewModel, ConnectionsPage>();
            _navigationService.AddPageViewModelBinding<ConnectionViewModel, ConnectionPage>();
            _navigationService.AddPageViewModelBinding<RegisterViewModel, RegisterPage>();
            _navigationService.AddPageViewModelBinding<AcceptInviteViewModel, AcceptInvitePage>();
            _navigationService.AddPageViewModelBinding<CredentialsViewModel, CredentialsPage>();
            _navigationService.AddPageViewModelBinding<CredentialViewModel, CredentialPage>();
            _navigationService.AddPageViewModelBinding<AccountViewModel, AccountPage>();
            _navigationService.AddPageViewModelBinding<CreateInvitationViewModel, CreateInvitationPage>();
            _navigationService.AddPageViewModelBinding<PhoneNumberViewModel, PhoneNumberPage>();
            _navigationService.AddPageViewModelBinding<CloudAgentsViewModel, CloudAgentsPage>();
            _navigationService.AddPageViewModelBinding<CloudAgentViewModel, CloudAgentPage>();
            _navigationService.AddPageViewModelBinding<ProofRequestsViewModel, ProofRequestsPage>();
            _navigationService.AddPageViewModelBinding<ProofRequestViewModel, ProofRequestPage>();

            _navigationService.AddPopupViewModelBinding<PassCodeViewModel, PassCodePage>();

            if (_contextProvider.AgentExists())
            {
                await _navigationService.NavigateToAsync<MainViewModel>();
            }
            else
            {
                await _navigationService.NavigateToAsync<RegisterViewModel>();
            }
        }

        protected override void OnStart()
        {
            #if !DEBUG 
                AppCenter.Start("ios=" + AppConstant.IosAnalyticsKey + ";" +
                                "android=" + AppConstant.AndroidAnalyticsKey + ";",
                        typeof(Analytics), typeof(Crashes));
            #endif
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
