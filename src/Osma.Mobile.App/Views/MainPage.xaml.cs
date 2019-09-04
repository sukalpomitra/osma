using System.Threading.Tasks;
using AgentFramework.Core.Contracts;
using Osma.Mobile.App.Services.Interfaces;
using Osma.Mobile.App.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Osma.Mobile.App.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage : TabbedPage, IRootView
	{
        private readonly ICustomAgentContextProvider _agentContextProvider;
        private readonly IProvisioningService _provisioningService;
        public MainPage (ICustomAgentContextProvider agentContextProvider,
            IProvisioningService provisioningService)
		{
			InitializeComponent ();
            _agentContextProvider = agentContextProvider;
            _provisioningService = provisioningService;
        }

        private void CurrentPageChanged(object sender, System.EventArgs e) => GetPageNameAsync(CurrentPage);

        private void Appearing(object sender, System.EventArgs e) => GetPageNameAsync(CurrentPage);

        private async Task GetPageNameAsync(Page page)
        {
            var context = await _agentContextProvider.GetContextAsync();
            var provisioningRecord = await _provisioningService.GetProvisioningAsync(context.Wallet);
            Title  = provisioningRecord.Owner.Name.ToUpperInvariant();
        }
    }
}

