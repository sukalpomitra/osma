namespace Osma.Mobile.App.Events
{
    public enum ApplicationEventType
    {
        ConnectionsUpdated,
        CloudAgentsUpdated,
        CredentialUpdated,
        ProofRequestUpdated,
        ProofRequestAtrributeUpdated
    }

    public class ApplicationEvent
    {
        public ApplicationEventType Type { get; set; }
    }
}
