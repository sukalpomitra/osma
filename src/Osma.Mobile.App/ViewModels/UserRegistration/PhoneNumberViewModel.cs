using System;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Models.Wallets;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Services.Models;
using Osma.Mobile.App.Views.UserRegistration;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels
{
    public class PhoneNumberViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;

        public PhoneNumberViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider) : base(
                                 nameof(PhoneNumberViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
        }

        #region Bindable Command
        public ICommand OpenCreateWalletPageCommand => new Command(async () => await _navigationService.NavigateToAsync<CreateWalletViewModel>());

        #endregion
    }
}
