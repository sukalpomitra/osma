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
    public class EmailAddressViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;

        public EmailAddressViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider) : base(
                                 nameof(EmailAddressViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
        }

        #region Bindable Command
        public ICommand OpenPhoneNumberPageCommand => new Command(async () => await _navigationService.NavigateToAsync<PhoneNumberViewModel>());

        #endregion
    }
}
