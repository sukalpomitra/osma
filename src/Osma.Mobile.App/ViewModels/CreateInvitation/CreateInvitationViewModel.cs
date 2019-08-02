using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Contracts;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;
using AgentFramework.Core.Extensions;

namespace Osma.Mobile.App.ViewModels.CreateInvitation
{
    public class CreateInvitationViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IConnectionService _connectionService;

        public CreateInvitationViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            ICustomAgentContextProvider agentContextProvider,
            IConnectionService defaultConnectionService
            ) : base(
                nameof(CreateInvitationViewModel),
                userDialogs,
                navigationService
           )
        {
            _agentContextProvider = agentContextProvider;         
            _connectionService = defaultConnectionService;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await base.InitializeAsync(navigationData);
        }

        private async Task CreateInvitation()
        {
            try
            {
                var context = await _agentContextProvider.GetContextAsync();
                var (invitation, _) = await _connectionService.CreateInvitationAsync(context);

                QrCodeValue = invitation.ServiceEndpoint + "?c_i=" + (invitation.ToJson().ToBase64());
            }
            catch (Exception ex)
            {
                await DialogService.AlertAsync(ex.Message);
            }
        }

        #region Bindable Command

        public ICommand CreateInvitationCommand => new Command(async () => await CreateInvitation());

        #endregion

        #region Bindable Properties

        private string _qrCodeValue;

        public string QrCodeValue
        {
            get => _qrCodeValue;
            set => this.RaiseAndSetIfChanged(ref _qrCodeValue, value);
        }

        #endregion
    }
}
