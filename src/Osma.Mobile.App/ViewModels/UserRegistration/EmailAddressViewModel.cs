using System;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Models.Wallets;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Services.Models;
using Osma.Mobile.App.Views.UserRegistration;
using ReactiveUI;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels
{
    public class EmailAddressViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;

        public EmailAddressViewModel(IUserDialogs userDialogs,
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider,
                                 string fullName
                                 ) : base(
                                 nameof(EmailAddressViewModel),
                                 userDialogs,
                                 navigationService)
        {
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
            FullName = fullName;
        }

        #region Bindable Command
        public ICommand OpenPhoneNumberPageCommand => new Command(async () => await _navigationService.NavigateToAsync<PhoneNumberViewModel>());

        #endregion

        #region Bindable Properties
        private string _emailAddress;

        public string EmailAddress
        {
            get => _emailAddress;
            set => this.RaiseAndSetIfChanged(ref _emailAddress, value);
        }

        private string _fullName;

        public string FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }
        #endregion
    }
}
