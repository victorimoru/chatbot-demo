using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using WorkerAssistant.Client.Data;
using WorkerAssistant.Client.Resources;
using WorkerAssistant.Client.Services;

namespace WorkerAssistant.Client.Shared
{
    public partial class Sidebar
    {
        public List<Conversation> AllConversations  { get; set; } = [];

        public Conversation ActiveConversation { get; set; } = default!;

        [Inject]
        private IConversationMediator Mediator { get; set; } = default!;

        [Inject]
        private IStringLocalizer<AppStrings> Localizer { get; set; } = default!;

        protected override void OnInitialized()
        {
            Mediator.ConversationSelected += HandleExternalConversationSelection;
            Mediator.NewConversationCreated += HandleNewConversationCreated;
        }

        private void StartNewConversation()
        {
            // Request the ChatContainer to create a new conversation
            Mediator.RequestNewConversation();
        }

        private void HandleNewConversationCreated(Conversation newConversation)
        {
            AllConversations.Insert(0,(newConversation));

            // Set it as active
            ActiveConversation = newConversation;
            StateHasChanged();
        }

        private void SelectConversation(Conversation conversation)
        {
            Mediator.SelectConversation(conversation);
        }

        private void HandleExternalConversationSelection(Conversation conversation)
        {
            if (conversation != ActiveConversation)
            {
                ActiveConversation = conversation;
                StateHasChanged();
            }
        }

        private static string GetStatusClass(ConversationStatus status)
        {
            return status switch
            {
                ConversationStatus.Active => "status-active",
                ConversationStatus.Pending => "status-pending",
                ConversationStatus.Closed => "status-closed",
                _ => ""
            };
        }

        public void Dispose()
        {
            Mediator.ConversationSelected -= HandleExternalConversationSelection;
            Mediator.NewConversationCreated -= HandleNewConversationCreated;
        }
    }
}



