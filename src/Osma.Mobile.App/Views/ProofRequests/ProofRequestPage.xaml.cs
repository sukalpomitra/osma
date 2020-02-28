using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Osma.Mobile.App.Views.ProofRequests
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofRequestPage : ContentPage
    {
        public ProofRequestPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
        }
    }
}
