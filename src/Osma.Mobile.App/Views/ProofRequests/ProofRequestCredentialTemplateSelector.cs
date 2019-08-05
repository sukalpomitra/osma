using Xamarin.Forms;

namespace Osma.Mobile.App.Views.ProofRequests
{
    public class ProofRequestCredentialTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return TextTemplate;
        }
    }
}
