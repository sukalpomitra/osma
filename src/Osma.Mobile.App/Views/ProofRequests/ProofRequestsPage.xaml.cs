using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Osma.Mobile.App.Views.ProofRequests
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofRequestsPage : ContentPage
    {
        public ProofRequestsPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            //InitializeComponent();
        }

        private void ToggleModalTapped(object sender, EventArgs e)
        {
            //moreModal.IsVisible = !moreModal.IsVisible;
        }
    }
}
