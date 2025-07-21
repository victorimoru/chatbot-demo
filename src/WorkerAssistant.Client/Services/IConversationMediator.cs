using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public interface IConversationMediator
    {
        event Action<Conversation> ConversationSelected;
        event Action NewConversationRequested; 
        event Action<Conversation> NewConversationCreated; 

        void SelectConversation(Conversation conversation);
        void RequestNewConversation(); 
        void NotifyNewConversationCreated(Conversation conversation); 
    }
}
