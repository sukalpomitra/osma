using System;
using System.Collections.Generic;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;

namespace Osma.Mobile.App.Views.Security
{
    public partial class PassCodePage : PopupPage
    {
        public PassCodePage()
        {
            InitializeComponent();
            passCode.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (passCode.Text.Length == 6)
                {
                    authorize.IsEnabled = true;
                }
                else
                {
                    authorize.IsEnabled = false;
                }
            };
        }
    }
}
