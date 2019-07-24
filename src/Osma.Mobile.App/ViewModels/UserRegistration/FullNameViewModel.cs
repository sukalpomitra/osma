using System;
using System.Threading.Tasks;
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
    public class FullNameViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly INavigationService _navigationService;

        public FullNameViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider) : base(
                                 nameof(FullNameViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _navigationService = navigationService;
            _agentContextProvider = agentContextProvider;
        }


        #region Bindable Property
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }

        #endregion

        #region Bindable Command    
        public ICommand OpenEmailAddressPageCommand => new Command(async () => await _navigationService.NavigateToAsync<EmailAddressViewModel>());

        #endregion
    }
}
