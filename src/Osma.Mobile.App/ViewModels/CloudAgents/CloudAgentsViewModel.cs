using System;
using System.Collections.Generic;
using System.Windows.Input;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;
using Xamarin.Forms;
using ReactiveUI;
using AgentFramework.Core.Models.Records;
using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using Autofac;

namespace Osma.Mobile.App.ViewModels.CloudAgents
{
    public class CloudAgentsViewModel : ABaseViewModel
    {
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly ICloudAgentRegistrationService _registrationService;
        private readonly ILifetimeScope _scope;
        private readonly IMessageService _messageService;

        public CloudAgentsViewModel(IUserDialogs userDialogs, 
                                 INavigationService navigationService,
                                 ICustomAgentContextProvider agentContextProvider,
                                 ICloudAgentRegistrationService registrationService,
                                 IMessageService messageService,
                                 ILifetimeScope scope) : base(
                                 nameof(CloudAgentsViewModel), 
                                 userDialogs, 
                                 navigationService)
        {
            _agentContextProvider = agentContextProvider;
            _registrationService = registrationService;
            _scope = scope;
            _messageService = messageService;
        }

        public override async Task InitializeAsync(object navigationData)
        {
            await RefreshCloudAgents();
            await base.InitializeAsync(navigationData);
        }

        public async Task RefreshCloudAgents()
        {
            RefreshingCloudAgents = true;

            var context = await _agentContextProvider.GetContextAsync();
            var agent = await _agentContextProvider.GetAgentAsync();

            var messages = await _messageService.ConsumeAsync(context.Wallet);
            foreach (var message in messages)
            {
                await agent.ProcessAsync(context, message);
            }
            
            RefreshingCloudAgents = false;
        }

        public async Task DeleteCloudAgent()
        {

        }

        #region Bindable Command
        public ICommand RefreshCommand => new Command(async () => await RefreshCloudAgents());

        public ICommand DeleteCommand => new Command(async () => await DeleteCloudAgent());
        
        #endregion

        #region Bindable Properties
        private List<CloudAgentRegistrationRecord> _cloudAgentsGrouped;
        public List<CloudAgentRegistrationRecord> CloudAgentsGrouped
        {
            get => _cloudAgentsGrouped;
            set => this.RaiseAndSetIfChanged(ref _cloudAgentsGrouped, value);
        }

        private bool _refreshingCloudAgents;
        public bool RefreshingCloudAgents
        {
            get => _refreshingCloudAgents;
            set => this.RaiseAndSetIfChanged(ref _refreshingCloudAgents, value);
        }

        #endregion
    }
}
