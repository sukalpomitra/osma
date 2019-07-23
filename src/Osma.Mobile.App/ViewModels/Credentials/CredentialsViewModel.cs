using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Records;
using Autofac;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Account;
using ReactiveUI;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using AgentFramework.Core.Extensions;

namespace Osma.Mobile.App.ViewModels.Credentials
{
    public class CredentialsViewModel : ABaseViewModel
    {
        private readonly ICredentialService _credentialService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ILifetimeScope _scope;
        private readonly IConnectionService _connectionService;

        public CredentialsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            ICredentialService credentialService,
            ICustomAgentContextProvider agentContextProvider,
            IConnectionService defaultConnectionService,
            ILifetimeScope scope
            ) : base(
                "Credentials",
                userDialogs,
                navigationService
           )
        {
            _credentialService = credentialService;
            _agentContextProvider = agentContextProvider;         
            _connectionService = defaultConnectionService;
            _scope = scope;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))   
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshCredentials();
            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshCredentials()
        {
            RefreshingCredentials = true;

            var context = await _agentContextProvider.GetContextAsync();
            var credentialsRecords = await _credentialService.ListAsync(context);

#if DEBUG
            credentialsRecords.Add(new CredentialRecord
            {
                ConnectionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialDefinitionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialRevocationId = Guid.NewGuid().ToString().ToLowerInvariant(),
                State = CredentialState.Issued,
            });
            credentialsRecords.Add(new CredentialRecord
            {
                ConnectionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialDefinitionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialRevocationId = Guid.NewGuid().ToString().ToLowerInvariant(),
                State = CredentialState.Issued,
            });
            credentialsRecords.Add(new CredentialRecord
            {
                ConnectionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialDefinitionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialId = Guid.NewGuid().ToString().ToLowerInvariant(),
                CredentialRevocationId = Guid.NewGuid().ToString().ToLowerInvariant(),
                State = CredentialState.Issued,
            });
#endif

            IList<CredentialViewModel> credentialsVms = new List<CredentialViewModel>();
            foreach (var credentialRecord in credentialsRecords)
            {
                CredentialViewModel credential = _scope.Resolve<CredentialViewModel>(new NamedParameter("credential", credentialRecord));
                credentialsVms.Add(credential);
            }

            var filteredCredentialVms = FilterCredentials(SearchTerm, credentialsVms);
            var groupedVms = GroupCredentials(filteredCredentialVms);
            CredentialsGrouped = groupedVms;

            Credentials.Clear();
            Credentials.InsertRange(filteredCredentialVms);

            HasCredentials = Credentials.Any();
            RefreshingCredentials = false;

        }

        public async Task SelectCredential(CredentialViewModel credential) => await NavigationService.NavigateToAsync(credential, null, NavigationType.Modal);

        private IEnumerable<CredentialViewModel> FilterCredentials(string term, IEnumerable<CredentialViewModel> credentials)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return credentials;
            }
            // Basic search
            var filtered = credentials.Where(credentialViewModel => credentialViewModel.CredentialName.Contains(term));
            return filtered;
        }

        private IEnumerable<Grouping<string, CredentialViewModel>> GroupCredentials(IEnumerable<CredentialViewModel> credentialViewModels)
        {
            var grouped = credentialViewModels
            .OrderBy(credentialViewModel => credentialViewModel.CredentialName)
            .GroupBy(credentialViewModel =>
            {
                if(string.IsNullOrWhiteSpace(credentialViewModel.CredentialName))
                {
                    return "*";
                }
                return credentialViewModel.CredentialName[0].ToString().ToUpperInvariant();
            }) // TODO check credentialName
            .Select(group =>
            {
                return new Grouping<string, CredentialViewModel>(group.Key, group.ToList());
            }
            );

            return grouped;

        }

        private async Task CreateInvitation()
        {
            DialogService.Alert("Invitation being created");

            var context = await _agentContextProvider.GetContextAsync();
            var (invitation, _)= await _connectionService.CreateInvitationAsync(context);
            
            string barcodeValue = invitation.ServiceEndpoint + "?c_i=" + (invitation.ToJson().ToBase64());

            //QRCodeGenerator(barcodeValue); //set a view attribute, to show on the screen. 

            QrCodeValue = barcodeValue;
        }

        private ZXingBarcodeImageView QRCodeGenerator(String barcodeValue)
        {
            var barcode = new ZXingBarcodeImageView
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                AutomationId = "zxingBarcodeImageView",
            };

            barcode.BarcodeFormat = ZXing.BarcodeFormat.QR_CODE;
            barcode.BarcodeOptions.Width = 300;
            barcode.BarcodeOptions.Height = 300;
            barcode.BarcodeOptions.Margin = 10;
            barcode.BarcodeValue = barcodeValue;

            return barcode;

        }

    #region Bindable Command

    public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());

        public ICommand SelectCredentialCommand => new Command<CredentialViewModel>(async (credentials) =>
        {
            if (credentials != null)
                await SelectCredential(credentials);
        });

        public ICommand RefreshCommand => new Command(async () => await RefreshCredentials());

        public ICommand CreateInvitationCommand => new Command(() => CreateInvitation());

        #endregion

        #region Bindable Properties
        private RangeEnabledObservableCollection<CredentialViewModel> _credentials = new RangeEnabledObservableCollection<CredentialViewModel>();
        public RangeEnabledObservableCollection<CredentialViewModel> Credentials
        {
            get => _credentials;
            set => this.RaiseAndSetIfChanged(ref _credentials, value);
        }

        private bool _hasCredentials;
        public bool HasCredentials
        {
            get => _hasCredentials;
            set => this.RaiseAndSetIfChanged(ref _hasCredentials, value);
        }

        private bool _refreshingCredentials;
        public bool RefreshingCredentials
        {
            get => _refreshingCredentials;
            set => this.RaiseAndSetIfChanged(ref _refreshingCredentials, value);
        }

        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
        }

        private string _qrCodeValue;

        public string QrCodeValue
        {
            get => _qrCodeValue;
            set => this.RaiseAndSetIfChanged(ref _qrCodeValue, value);
        }

        private IEnumerable<Grouping<string, CredentialViewModel>> _credentialsGrouped;
        public IEnumerable<Grouping<string, CredentialViewModel>> CredentialsGrouped
        {
            get => _credentialsGrouped;
            set => this.RaiseAndSetIfChanged(ref _credentialsGrouped, value);
        }

        #endregion
    }
}
