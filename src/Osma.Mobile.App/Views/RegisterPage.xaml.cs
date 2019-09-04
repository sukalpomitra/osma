using System.Threading.Tasks;
using Osma.Mobile.App.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Acr.UserDialogs;
using Osma.Mobile.App.Services.Interfaces;

namespace Osma.Mobile.App.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RegisterPage : ContentPage, IRootView
    {

        public RegisterPage ()
		{
			InitializeComponent ();
            passCode.TextChanged += (object sender, TextChangedEventArgs e) => {
                showCreateWalletButton();
            };

            fullName.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (fullName.Text.Length > 0)
                {
                    if (passCode.IsVisible)
                    {
                        showCreateWalletButton();
                    } else
                    {
                        createWallet.IsVisible = true;
                    }
                } else
                {
                    createWallet.IsVisible = false;
                }
            };

        }

        private void showCreateWalletButton()
        {
            if (passCode.Text.Length == 6 && fullName.Text.Length > 0)
            {
                createWallet.IsVisible = true;
            }
            else
            {
                createWallet.IsVisible = false;
            }
        }
    }
}