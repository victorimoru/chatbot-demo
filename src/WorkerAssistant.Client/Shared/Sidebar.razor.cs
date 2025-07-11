using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WorkerAssistant.Client.Data;

namespace WorkerAssistant.Client.Shared
{
    public partial class Sidebar
    {
        private ElementReference messagesContainer;
        private Timer typingTimer;
        private string CurrentMessage { get; set; } = string.Empty;
        private bool IsTyping { get; set; } = false;
        private Conversation ActiveConversation { get; set; }
        private List<Conversation> Conversations { get; set; } =
    [
        new Conversation
        {
            Id = 1,
            Title = "New Conversation",
            Icon = "fas fa-comment",
            Status = ConversationStatus.Active,
            LastMessagePreview = "Need help configuring the build pipeline...",
            LastMessageTime = DateTime.Now,
            Messages = new List<Message>
            {
                new Message
                {
                    Text = "Hello! I'm your Worker Assistant. I can help with technical questions, code reviews, and project guidance.",
                    IsUser = false,
                    Timestamp = DateTime.Now.AddMinutes(-10)
                },
                new Message
                {
                    Text = "How do I set up a new Blazor WASM project?",
                    IsUser = true,
                    Timestamp = DateTime.Now.AddMinutes(-5)
                },
                new Message
                {
                    Text = "You can create a new Blazor WASM project using the command: dotnet new blazorwasm -o YourProjectName",
                    IsUser = false,
                    Timestamp = DateTime.Now.AddMinutes(-4)
                }
            }
        },
        //new Conversation
        //{
        //    Id = 2,
        //    Title = "Bug Report",
        //    Icon = "fas fa-bug",
        //    Status = ConversationStatus.Pending,
        //    LastMessagePreview = "Null reference in login service...",
        //    LastMessageTime = DateTime.Now.AddDays(-1),
        //    Messages = new List<Message>
        //    {
        //        new Message
        //        {
        //            Text = "I'm getting a null reference exception in the login service",
        //            IsUser = true,
        //            Timestamp = DateTime.Now.AddDays(-1).AddHours(-3)
        //        },
        //        new Message
        //        {
        //            Text = "Could you share the error details and code snippet?",
        //            IsUser = false,
        //            Timestamp = DateTime.Now.AddDays(-1).AddHours(-2)
        //        }
        //    }
        //},
        //new Conversation
        //{
        //    Id = 3,
        //    Title = "API Design",
        //    Icon = "fas fa-code",
        //    Status = ConversationStatus.Closed,
        //    LastMessagePreview = "Discussion about REST endpoints...",
        //    LastMessageTime = DateTime.Now.AddDays(-3),
        //    Messages = new List<Message>
        //    {
        //        new Message
        //        {
        //            Text = "What's the best way to design REST endpoints for our project?",
        //            IsUser = true,
        //            Timestamp = DateTime.Now.AddDays(-3).AddHours(-5)
        //        },
        //        new Message
        //        {
        //            Text = "For REST APIs, I recommend following the resource-oriented design pattern...",
        //            IsUser = false,
        //            Timestamp = DateTime.Now.AddDays(-3).AddHours(-4)
        //        }
        //    }
        //}
    ];

        private void SelectConversation(Conversation conversation)
        {
            ActiveConversation = conversation;
            CurrentMessage = "";
            StateHasChanged();
        }

        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    if (firstRender)
        //    {
        //        typingTimer = new System.Timers.Timer(1500);
        //        typingTimer.Elapsed += (sender, e) =>
        //        {
        //            IsTyping = false;
        //            typingTimer.Stop();
        //            InvokeAsync(StateHasChanged);
        //        };
        //        typingTimer.AutoReset = false;

        //        // Select first conversation by default
        //        if (Conversations.Any())
        //        {
        //            SelectConversation(Conversations.First());
        //        }
        //    }

        //    await ScrollToBottom();
        //}

        private async Task HandleKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(CurrentMessage))
            {
                //await SendMessage();
            }
        }

        private string GetStatusClass(ConversationStatus status)
        {
            return status switch
            {
                ConversationStatus.Active => "status-active",
                ConversationStatus.Pending => "status-pending",
                ConversationStatus.Closed => "status-closed",
                _ => ""
            };
        }
        private void StartNewConversation()
        {
            var newConversation = new Conversation
            {
                Id = Conversations.Max(c => c.Id) + 1,
                Title = "New Conversation",
                Icon = "fas fa-comment",
                Status = ConversationStatus.Active,
                LastMessagePreview = "New conversation started...",
                LastMessageTime = DateTime.Now,
                Messages =
                    [
                        new Message
                        {
                            Text = "Hello! I'm your Worker Assistant. How can I help you today?",
                            IsUser = false,
                            Timestamp = DateTime.Now
                        }
                    ]
            };

            Conversations.Insert(0, newConversation);
            SelectConversation(newConversation);
        }
    }
}