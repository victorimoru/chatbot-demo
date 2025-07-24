using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    public interface IConversationMediator
    {
        event Action<Conversation> ConversationSelected;
        event Action NewConversationRequested; 
        event Action<Conversation> NewConversationCreated;
        event Action InitializationRequested;
        event Action InitializationCompleted;


        void RequestInitialization();
        void NotifyInitializationCompleted();
        void SelectConversation(Conversation conversation);
        void RequestNewConversation(); 
        void NotifyNewConversationCreated(Conversation conversation); 
    }
}
