using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    // IConversationMediator.cs
    public interface IConversationMediator
    {
        event Action<Conversation> ConversationSelected;
        event Action NewConversationRequested; // New event for requesting conversation creation
        event Action<Conversation> NewConversationCreated; // Event for new conversation creation

        void SelectConversation(Conversation conversation);
        void RequestNewConversation(); // Method to request new conversation
        void NotifyNewConversationCreated(Conversation conversation); // Method to notify about created conversation
    }
}
