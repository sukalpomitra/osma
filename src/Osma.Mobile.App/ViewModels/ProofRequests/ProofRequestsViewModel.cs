using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Proofs;
using AgentFramework.Core.Models.Records;
using Autofac;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using Osma.Mobile.App.ViewModels.Account;
using Osma.Mobile.App.ViewModels.CloudAgents;
using Osma.Mobile.App.ViewModels.CreateInvitation;
using ReactiveUI;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofRequestsViewModel : ABaseViewModel
    {
        private readonly IProofService _proofService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ILifetimeScope _scope;
        private readonly IMessageService _messageService;

        public ProofRequestsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            ICustomAgentContextProvider agentContextProvider,
            IMessageService messageService,
            ILifetimeScope scope    
            ) : base(
                nameof(ProofRequestsViewModel),
                userDialogs,
                navigationService
           )
        {

            _proofService = proofService;
            _agentContextProvider = agentContextProvider;
            _messageService = messageService;
            _scope = scope;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshProofs();
            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshProofs()
        {
            RefreshingProofs = true;

            var context = await _agentContextProvider.GetContextAsync();
            var proofRecords = await _proofService.ListAsync(context);
            ProofCount = "we have number of records:" + proofRecords.Count;

            //IList<CredentialViewModel> credentialsVms = new List<CredentialViewModel>();
            //foreach (var credentialRecord in proofRecords)
            //{
            //    CredentialViewModel credential = _scope.Resolve<CredentialViewModel>(new NamedParameter("credential", credentialRecord));
            //    credentialsVms.Add(credential);
            //}

            //var filteredCredentialVms = FilterCredentials(SearchTerm, credentialsVms);
            //var groupedVms = GroupCredentials(filteredCredentialVms);
            //CredentialsGrouped = groupedVms;

            //Credentials.Clear();
            //Credentials.InsertRange(filteredCredentialVms);

            //HasProofs = Credentials.Any();
            HasProofs = true;
            RefreshingProofs = false;
            ProofsGrouped = proofRecords;

        }

        #region Bindable Command
        
        public ICommand RefreshCommand => new Command(async () => await RefreshProofs());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand CloudAgentsCommand => new Command(async () => await NavigationService.NavigateToAsync<CloudAgentsViewModel>());

        public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());


        #endregion

        #region Bindable Properties

        private bool _hasCredentials;
        public bool HasProofs
        {
            get => _hasCredentials;
            set => this.RaiseAndSetIfChanged(ref _hasCredentials, value);
        }

        private bool _refreshingCredentials;
        public bool RefreshingProofs
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

        private List<ProofRecord> _proofsGrouped;

        public List<ProofRecord> ProofsGrouped
        {
            get => _proofsGrouped;
            set => this.RaiseAndSetIfChanged(ref _proofsGrouped, value);
        }

        private String _proofCount;

        public String ProofCount
        {
            get => _proofCount;
            set => this.RaiseAndSetIfChanged(ref _proofCount, value);
        }


        #endregion
    }
}