using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Services
{
    // ConversationMediator.cs (implementation)
    public class ConversationMediator : IConversationMediator
    {
        public event Action<Conversation> ConversationSelected;
        public event Action NewConversationRequested;
        public event Action<Conversation> NewConversationCreated;

        public void SelectConversation(Conversation conversation)
        {
            ConversationSelected?.Invoke(conversation);
        }

        public void RequestNewConversation()
        {
            NewConversationRequested?.Invoke();
        }

        public void NotifyNewConversationCreated(Conversation conversation)
        {
            NewConversationCreated?.Invoke(conversation);
        }
    }
}
