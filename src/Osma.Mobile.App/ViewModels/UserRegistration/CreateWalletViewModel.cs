using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Views.UserRegistration;
using System.Windows.Input;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels
{
    public class CreateWalletViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;

        public CreateWalletViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider) : base(
                                 nameof(CreateWalletViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
        }

    }
}
