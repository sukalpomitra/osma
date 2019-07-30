using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Osma.Mobile.App.Views.Credentials
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CredentialPage : ContentPage
    {
        public CredentialPage()
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
