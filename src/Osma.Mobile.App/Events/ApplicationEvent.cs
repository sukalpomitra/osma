namespace Osma.Mobile.App.Events
{
    public enum ApplicationEventType
    {
        ConnectionsUpdated,
        CloudAgentsUpdated,
        CredentialUpdated
    }

    public class ApplicationEvent
    {
        public ApplicationEventType Type { get; set; }
    }
}
