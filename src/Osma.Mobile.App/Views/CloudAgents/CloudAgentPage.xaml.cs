using System;
using Xamarin.Forms;

namespace Osma.Mobile.App.Views.CloudAgents
{
    public partial class CloudAgentPage : ContentPage
    {

        public CloudAgentPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
        }

        private void ToggleModalTapped(object sender, EventArgs e)
        {
            moreModal.IsVisible = !moreModal.IsVisible;
        }
    }
}
