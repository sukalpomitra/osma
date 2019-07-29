using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using ReactiveUI;
using Xamarin.Forms;
using AgentFramework.Core.Models.Records;
using AgentFramework.Core.Contracts;
using System.Threading.Tasks;

namespace Osma.Mobile.App.ViewModels.Credentials
{
    public class CredentialViewModel : ABaseViewModel
    {
        private readonly CredentialRecord _credential;

        private readonly ICredentialService _credentialService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IMessageService _messageService;

        public CredentialViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            ICredentialService credentialService,
            ICustomAgentContextProvider agentContextProvider,
            IMessageService messageService,
            CredentialRecord credential
        ) : base(
            nameof(CredentialViewModel),
            userDialogs,
            navigationService
        )
        {
            _credential = credential;
            _credentialService = credentialService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;

            string schemaId = credential.SchemaId;
            CredentialName = (schemaId.Split(':')[2]).Replace(" schema", "") + " - " + (schemaId.Split(':')[3]);
            CredentialSubtitle = credential.State.ToString();

            _isNew = IsCredentialNew(_credential);
        }
        private bool IsCredentialNew(CredentialRecord credential)
        {
            // TODO OS-200, Currently a Placeholder for a mix of new and not new cells
            Random random = new Random();
            return random.Next(0, 2) == 1;
        }

        private async Task CreateCredentialRequest()
        {
            var context = await _agentContextProvider.GetContextAsync();
            var (msg, rec) = await _credentialService.CreateCredentialRequestAsync(context, _credential.Id);
            var rsp = await _messageService.SendAsync(context.Wallet, msg, rec);
        }

        #region Bindable Command

        public ICommand NavigateBackCommand => new Command(async () =>
        {
            await NavigationService.PopModalAsync();
        });

        public ICommand AcceptCredentialCommand => new Command(async () => { await CreateCredentialRequest(); });

        #endregion

        #region Bindable Properties

        private string _credentialName;
        public string CredentialName
        {
            get => _credentialName;
            set => this.RaiseAndSetIfChanged(ref _credentialName, value);
        }

        private string _credentialType;
        public string CredentialType
        {
            get => _credentialType;
            set => this.RaiseAndSetIfChanged(ref _credentialType, value);
        }

        private string _credentialImageUrl;
        public string CredentialImageUrl
        {
            get => _credentialImageUrl;
            set => this.RaiseAndSetIfChanged(ref _credentialImageUrl, value);
        }

        private string _credentialSubtitle;
        public string CredentialSubtitle
        {
            get => _credentialSubtitle;
            set => this.RaiseAndSetIfChanged(ref _credentialSubtitle, value);
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set => this.RaiseAndSetIfChanged(ref _isNew, value);
        }

        private string _qRImageUrl;
        public string QRImageUrl
        {
            get => _qRImageUrl;
            set => this.RaiseAndSetIfChanged(ref _qRImageUrl, value);
        }

        private IEnumerable<CredentialAttribute> _attributes;
        public IEnumerable<CredentialAttribute> Attributes
        {
            get => _attributes;
            set => this.RaiseAndSetIfChanged(ref _attributes, value);
        }

        #endregion
    }
}
