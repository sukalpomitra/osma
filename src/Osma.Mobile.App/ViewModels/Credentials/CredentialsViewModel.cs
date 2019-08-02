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
using Osma.Mobile.App.Events;
using Osma.Mobile.App.Extensions;
using Osma.Mobile.App.Services;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.Utilities;
using ReactiveUI;
using Xamarin.Forms;

namespace Osma.Mobile.App.ViewModels.Credentials
{
    public class CredentialsViewModel : ABaseViewModel
    {
        private readonly ICredentialService _credentialService;
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ILifetimeScope _scope;
        private readonly IEventAggregator _eventAggregator;

        public CredentialsViewModel(
            IUserDialogs userDialogs,
            INavigationService navigationService,
            ICredentialService credentialService,
            ICustomAgentContextProvider agentContextProvider,
            IEventAggregator eventAggregator,
            ILifetimeScope scope
            ) : base(
                nameof(CredentialsViewModel),
                userDialogs,
                navigationService
           )
        {
            _credentialService = credentialService;
            _agentContextProvider = agentContextProvider;
            _eventAggregator = eventAggregator;
            _scope = scope;

            this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .InvokeCommand(RefreshCommand);
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshCredentials();

            _eventAggregator.GetEventByType<ApplicationEvent>()
                           .Where(_ => _.Type == ApplicationEventType.CredentialUpdated)
                           .Subscribe(async _ => await RefreshCredentials());

            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshCredentials()
        {
            RefreshingCredentials = true;

            var context = await _agentContextProvider.GetContextAsync();
            var credentialsRecords = await _credentialService.ListAsync(context);

            credentialsRecords = credentialsRecords
                .Where(c => c.State == CredentialState.Offered || c.State == CredentialState.Issued)
                .ToList();

            var credentialsVms = credentialsRecords
                .Select(c => _scope.Resolve<CredentialViewModel>(new NamedParameter("credential", c)))
                .ToList();

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
            if (string.IsNullOrWhiteSpace(term)) return credentials;
            return credentials.Where(credentialViewModel => credentialViewModel.CredentialName.Contains(term));
        }

        private IEnumerable<Grouping<string, CredentialViewModel>> GroupCredentials(IEnumerable<CredentialViewModel> credentialViewModels)
        {
            return credentialViewModels
                .OrderBy(credentialViewModel => credentialViewModel.CredentialName)
                .GroupBy(credentialViewModel => {
                        if (string.IsNullOrWhiteSpace(credentialViewModel.CredentialName)) return "*";
                        return credentialViewModel.CredentialName[0].ToString().ToUpperInvariant();
                }) // TODO check credentialName
                .Select(group => {
                    return new Grouping<string, CredentialViewModel>(group.Key, group.ToList());
                });
        }

        #region Bindable Command
        public ICommand SelectCredentialCommand => new Command<CredentialViewModel>(async (credentials) =>
        {
            if (credentials != null) await SelectCredential(credentials);
        });

        public ICommand RefreshCommand => new Command(async () => await RefreshCredentials());

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

        private IEnumerable<Grouping<string, CredentialViewModel>> _credentialsGrouped;
        public IEnumerable<Grouping<string, CredentialViewModel>> CredentialsGrouped
        {
            get => _credentialsGrouped;
            set => this.RaiseAndSetIfChanged(ref _credentialsGrouped, value);
        }

        #endregion
    }
}