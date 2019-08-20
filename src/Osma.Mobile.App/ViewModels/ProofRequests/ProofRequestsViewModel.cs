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
using Osma.Mobile.App.Events;
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
        private readonly IEventAggregator _eventAggregator;

        public ProofRequestsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            IProofService proofService,
            ICustomAgentContextProvider agentContextProvider,
            IMessageService messageService,
            IEventAggregator eventAggregator,
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
            _eventAggregator = eventAggregator;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshProofs();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                           .Where(_ => _.Type == ApplicationEventType.ProofRequestUpdated)
                           .Subscribe(async _ => await RefreshProofs());

            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshProofs()
        {
            RefreshingProofRequests = true;

            var context = await _agentContextProvider.GetContextAsync();
            var proofRecords = await _proofService.ListAsync(context);

            var proofsVms = proofRecords
                .Select(p => _scope.Resolve<ProofRequestViewModel>(new NamedParameter("proof", p)))
                .ToList();

            var filteredProofVms = FilterProofRequests(SearchTerm, proofsVms);
            var groupedVms = GroupProofRequests(filteredProofVms);

            ProofRequestsGrouped = groupedVms;
            ProofRequestsCount = "we have number of records:" + proofRecords.Count;
            HasProofRequests = ProofRequests.Any();

            ProofRequests.Clear();
            ProofRequests.InsertRange(filteredProofVms);

            RefreshingProofRequests = false;
        }

        public async Task SelectProofRequest(ProofRequestViewModel proof) => await NavigationService.NavigateToAsync(proof, null, NavigationType.Modal);

        private IEnumerable<ProofRequestViewModel> FilterProofRequests(string term, IEnumerable<ProofRequestViewModel> proofs)
        {
            if (string.IsNullOrWhiteSpace(term)) return proofs;
            return proofs.Where(proofRequestViewModel => proofRequestViewModel.ProofName.Contains(term));
        }

        private IEnumerable<Grouping<string, ProofRequestViewModel>> GroupProofRequests(IEnumerable<ProofRequestViewModel> proofRequestsViewModels)
        {
            return proofRequestsViewModels
                .OrderBy(proofRequestsViewModel => proofRequestsViewModel.ProofName)
                .GroupBy(proofRequestsViewModel => {
                    if (string.IsNullOrWhiteSpace(proofRequestsViewModel.ProofName)) return "*";
                    return proofRequestsViewModel.ProofName[0].ToString().ToUpperInvariant();
                }) // TODO check proofRequestName
                .Select(group => {
                    return new Grouping<string, ProofRequestViewModel>(group.Key, group.ToList());
                });
        }

        #region Bindable Command
        public ICommand SelectProofRequestCommand => new Command<ProofRequestViewModel>(async (proofs) =>
        {
            if (proofs != null) await SelectProofRequest(proofs);
        });

        public ICommand RefreshCommand => new Command(async () => await RefreshProofs());

        public ICommand CreateInvitationCommand => new Command(async () => await NavigationService.NavigateToAsync<CreateInvitationViewModel>());

        public ICommand CloudAgentsCommand => new Command(async () => await NavigationService.NavigateToAsync<CloudAgentsViewModel>());

        public ICommand CheckAccountCommand => new Command(async () => await NavigationService.NavigateToAsync<AccountViewModel>());


        #endregion

        #region Bindable Properties

        private RangeEnabledObservableCollection<ProofRequestViewModel> _proofRequests = new RangeEnabledObservableCollection<ProofRequestViewModel>();
        public RangeEnabledObservableCollection<ProofRequestViewModel> ProofRequests
        {
            get => _proofRequests;
            set => this.RaiseAndSetIfChanged(ref _proofRequests, value);
        }

        private bool _hasProofRequests;
        public bool HasProofRequests
        {
            get => _hasProofRequests;
            set => this.RaiseAndSetIfChanged(ref _hasProofRequests, value);
        }

        private bool _refreshingProofRequests;
        public bool RefreshingProofRequests
        {
            get => _refreshingProofRequests;
            set => this.RaiseAndSetIfChanged(ref _refreshingProofRequests, value);
        }

        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
        }

        private IEnumerable<Grouping<string, ProofRequestViewModel>> _proofRequestsGrouped;
        public IEnumerable<Grouping<string, ProofRequestViewModel>> ProofRequestsGrouped
        {
            get => _proofRequestsGrouped;
            set => this.RaiseAndSetIfChanged(ref _proofRequestsGrouped, value);
        }

        private string _proofRequestsCount;
        public string ProofRequestsCount
        {
            get => _proofRequestsCount;
            set => this.RaiseAndSetIfChanged(ref _proofRequestsCount, value);
        }

        #endregion
    }
}