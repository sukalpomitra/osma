using System;
using Osma.Mobile.App.ViewModels.ProofRequests;
using Xamarin.Forms;

namespace Osma.Mobile.App.Views.ProofRequests
{
    public enum ProofRequestAttributeType
    {
        None,
        Text = 1,
        File = 2, 
        Name = 3
    }

    public class ProofRequestAttributeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate ErrorTemplate { get; set; }
        public DataTemplate NameTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {

            if (item is null)
            {
                return ErrorTemplate;
            }

            ProofRequestAttributeType proofRequestAttributeType;
            var proofRequestAttribute = item as ProofAttribute;


            if (proofRequestAttribute is null)
            {
                return ErrorTemplate;
            }

            try
            {
                proofRequestAttributeType = (ProofRequestAttributeType)Enum.Parse(typeof(ProofRequestAttributeType), proofRequestAttribute.Type, true);
                if (proofRequestAttribute.Value == null) proofRequestAttributeType = ProofRequestAttributeType.Name;
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Proof Request Attribute Type is Invalid");
            }
            switch (proofRequestAttributeType)
            {
                case ProofRequestAttributeType.Text:
                    return TextTemplate;
                case ProofRequestAttributeType.File:
                    return FileTemplate;
                case ProofRequestAttributeType.Name:
                    return NameTemplate;
                default:
                    return ErrorTemplate;

            }
        }
    }
}
