using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Reflection.Metadata;
using WorkerAssistant.Client.Data;
using WorkerAssistant.Client.Resources;
using WorkerAssistant.Client.Services;

namespace WorkerAssistant.Client.Shared
{
    public partial class ChatContainer
    {
        [Inject]
        private IVectorStoreService VectorStoreService { get; set; } = default!;

        [Inject]
        private HttpClient HttpClient { get; set; } = default!;

        [Inject]
        private ILanguageService LanguageService { get; set; } = default!;

        [Inject]
        private ILLMInteropService LLMInteropService { get; set; } = default!;

        [Inject]
        private IStringLocalizer<AppStrings> Localizer { get; set; } = default!;

        [Inject]
        private IConversationMediator Mediator  { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        private ElementReference messagesContainer;
        private string CurrentMessage { get; set; } = string.Empty;
        private Conversation? ActiveConversation;

        private bool IsInputDisabled { get; set; } = false;
        private bool IsSendDisabled => string.IsNullOrWhiteSpace(CurrentMessage) || IsInputDisabled || IsLLMResponding;
        private bool IsLLMResponding { get; set; } = false;
        private bool IsLLMThinking { get; set; } = false;

        private bool isListening = false;
        private string currentPrompt = string.Empty;
        private DotNetObjectReference<ChatContainer> objRef;

        protected override async Task OnInitializedAsync()
        {
            Mediator.ConversationSelected += HandleConversationSelected;
            Mediator.NewConversationRequested += HandleNewConversationRequest;
            LanguageService.OnLanguageChanged += HandleLanguageChange;

            objRef = DotNetObjectReference.Create(this);
            await LLMInteropService.InitializeSpeechRecognitionAsync(objRef);
        }

        private void HandleLanguageChange()
        {
            InvokeAsync(StateHasChanged);
        }

        private void SetLanguage(string langCode)
        {
            if (LanguageService.CurrentLanguage != langCode)
            {
                Console.WriteLine($"{langCode} selected, reloading page...");
                LanguageService.SetLanguage(langCode);
               // NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                NavigationManager.NavigateTo($"/{langCode}", forceLoad: true);
            }
            else
            {
                Console.WriteLine($"{langCode} is already the current language. No reload needed.");
            }
        }

        private string GetPlaceholderMessage()
        {
            return Localizer["PlaceHolderMessage"];
        }

        private void HandleConversationSelected(Conversation conversation)
        {
            ActiveConversation = conversation;
            ResetChatState();
            StateHasChanged();
        }

        private void HandleNewConversationRequest()
        {
            var newMessage = Localizer["WelcomeMessage"];
            var newconversation = Localizer["NewConversationButton"];

            ActiveConversation = new Conversation
            {
                Id = GenerateNewId(), 
                Title = $"{newconversation}",
                LLMResponses =
                [
                  new ChatMessage("assistant", $"{newMessage}", [], DateTime.Now, true)
                ],
                LastMessagePreview = "",
                LastMessageTime = DateTime.Now
            };

            ResetChatState();
            
            Mediator.NotifyNewConversationCreated(ActiveConversation);
            StateHasChanged();
        }

        private static int GenerateNewId()
        {
            return DateTime.Now.Ticks.GetHashCode();
        }

        private void ResetChatState()
        {
            CurrentMessage = string.Empty;
            IsLLMResponding = false;
            IsLLMThinking = false;
            IsInputDisabled = false;
        }

        private async Task HandleKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey)
            {
                await SendMessage();
            }
        }

        private async Task UpdateInputDisabledState()
        {
            await LLMInteropService.ToggleElementDisabledAsync("chat-input-area-id", IsInputDisabled);
            await LLMInteropService.ToggleElementDisabledAsync("send-button-id", IsInputDisabled); 
            await LLMInteropService.ToggleElementDisabledAsync("send-button-mic-id", IsInputDisabled);
            await LLMInteropService.ToggleElementVisibilityAsync("new-conversation-id", IsInputDisabled);
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage)) return;

            var userPrompt = CurrentMessage.Trim();

            ActiveConversation?.LLMResponses.Add(new ChatMessage("user", userPrompt, [], DateTime.Now));
            ActiveConversation.LastMessagePreview = userPrompt.Length > 30 ? string.Concat(userPrompt.AsSpan(0, 30), "...") : userPrompt;
            ActiveConversation.LastMessageTime = DateTime.Now;

            CurrentMessage = "";
            IsLLMResponding = true;
            IsLLMThinking = true;
            IsInputDisabled = true;

            await UpdateInputDisabledState();
            StateHasChanged(); // Show user message immediately

            await ScrollToBottom();

            try
            {
                var langCode = LanguageService.CurrentLanguage; 
                var promptFileName = $"prompt_template_qwen_{langCode}.txt";
                Console.WriteLine("Prompt Template Used: " + promptFileName);
                var promptTemplate = await HttpClient.GetStringAsync(promptFileName);

                // 2. Retrieve relevant chunks from the vector store
                var queryEmbedding = await LLMInteropService.GetEmbeddingAsync(userPrompt);
                var contextForPrompt = "";
                List<DocumentChunk> relevantChunks = [];

                if (langCode == "ru")
                {
                    var similiarChunks = await VectorStoreService.FindSimilarChunksNewRussianAsync(queryEmbedding, userPrompt, count: 8);
                    contextForPrompt = similiarChunks.Item1;
                    relevantChunks = similiarChunks.Item2;
                }
                else
                {
                    relevantChunks = await VectorStoreService.FindSimilarChunksNewAsync(queryEmbedding, userPrompt, count: 10);
                    contextForPrompt = string.Join("\n\n", relevantChunks.Select(c => c.Content));
                }
                 
                var sourcesForDisplay = relevantChunks.Select(c => c.Source).Take(3).ToList();

                ActiveConversation.Title = sourcesForDisplay.FirstOrDefault();

                // Add the placeholder for the assistant's response, including the sources
                var assistantMessage = new ChatMessage("assistant", "", sourcesForDisplay, DateTime.Now);
                ActiveConversation.LLMResponses.Add(assistantMessage);

                // 4. Create the final prompt by replacing the placeholders
                var finalPrompt = promptTemplate
                    .Replace("{retrieved_chunks_from_your_knowledge_base}", contextForPrompt)
                    .Replace("{the_user's_actual_question}", userPrompt);

                // 5. Fomart the final prompt for LLM
                var messagesForAI = new List<ChatMessage> { new("user", finalPrompt, [], DateTime.Now) };

                var sw = Stopwatch.StartNew();
                Console.WriteLine($"Stopwatch started for LLM response.......");

                // 6. Generate the response with the final prompt
                await LLMInteropService.CompleteStreamAsync(
                    messagesForAI,
                    onChunkReceived: async (chunk) =>
                    {
                        IsLLMThinking = false;
                        var content = chunk.Choices.FirstOrDefault()?.Delta.Content;
                        if (content != null)
                        {
                            var lastMessage = ActiveConversation.LLMResponses.Last();

                            if (string.IsNullOrEmpty(lastMessage.Content))
                            {
                                content = content.TrimStart()
                                    .Replace("According to the context, ", "")
                                    .Replace("According to the SOURCE DOCUMENT, ", "")
                                    .Replace("The source document indicates that ", "")
                                    .Replace("The context does not provide any information about", "I'm sorry, but I don't have that information.")
                                    .Replace("the context explicitly says that ", "")
                                    .Replace("the CONTEXT indicates that ", "")
                                    .Replace("the CONTEXT clearly states that ", "");

                                // This handles cases like "Yes, it is important... According to the SOURCE DOCUMENT, you must..."
                                // by splitting on the unwanted phrase and taking the part before it.
                                if (content.Contains("According to the SOURCE DOCUMENT"))
                                {
                                    content = content.Split(new[] { "According to the SOURCE DOCUMENT" }, StringSplitOptions.None)[0].Trim();
                                }
                            }

                            var updatedMessage = lastMessage with { Content = lastMessage.Content + content, TimeStamp = DateTime.Now };
                            ActiveConversation.LLMResponses[^1] = updatedMessage;
                            await InvokeAsync(StateHasChanged);
                        }
                    },
                    onStreamCompleted: async () =>
                    {
                        var lastMessage = ActiveConversation.LLMResponses.Last();
                        var messyContent = lastMessage.Content;

                        var cleanedContent = messyContent.TrimStart()
                            .Replace("According to the context, ", "")
                            .Replace("According to the SOURCE DOCUMENT, ", "")
                            .Replace("The source document indicates that ", "")
                            .Replace("The SOURCE DOCUMENT indicates that the ", "The") 
                            .Replace("Sure, the SOURCE DOCUMENT indicates that the", "Sure, the")
                            .Replace("Sure, the SOURCE DOCUMENT states that you", "Sure, you") 
                            .Replace("Sure, the SOURCE DOCUMENT specifies that the", "Sure, the")
                            .Replace("Sure, the SOURCE DOCUMENT specifies that you", "Sure, you")
                            .Replace("the SOURCE DOCUMENT does not allow", "")
                            .Replace("The context does not provide any information about", "I'm sorry, but I don't have that information.")
                            .Replace("the context explicitly says that ", "")
                            .Replace("the CONTEXT indicates that ", "")
                            .Replace("According to the information, ", "")
                            .Replace("the information indicates that", "")
                            .Replace("the CONTEXT clearly states that ", "");

                        // --- NEW: Enforce Sentence Case ---
                        if (!string.IsNullOrEmpty(cleanedContent))
                        {
                            // Capitalize the very first letter and append the rest of the string.
                            cleanedContent = char.ToUpper(cleanedContent[0]) + cleanedContent.Substring(1);
                        }
                        // --- END OF NEW BLOCK ---

                        var finalMessage = lastMessage with { Content = cleanedContent };
                        ActiveConversation.LLMResponses[^1] = finalMessage;

                        Console.WriteLine($"Response Generation takes {sw.ElapsedMilliseconds} ms");
                        sw.Stop();

                        IsLLMResponding = false;
                        IsInputDisabled = false;
                        await UpdateInputDisabledState();
                        await InvokeAsync(StateHasChanged);
                        await LLMInteropService.HighlightCodeAsync();
                    }
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error sending message: {ex.Message}");
                IsLLMResponding = false;
                IsInputDisabled = false;

                var lastMessage = ActiveConversation.LLMResponses.Last();
                var messyContent = lastMessage.Content;
                var finalMessage = lastMessage with { Content = $"Error sending message: {ex.Message}. Please wait a moment and retry!" };
                ActiveConversation.LLMResponses[^1] = finalMessage;

                StateHasChanged();
                return;
            }

            IsLLMResponding = false;
            StateHasChanged();
            // await ScrollToBottom();
        }
        private async Task ScrollToBottom()
        {
            await JsRuntime.InvokeVoidAsync("scrollToBottom", messagesContainer);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ScrollToBottom();
               // await jsRuntime.InvokeVoidAsync("updateSliderPosition", LanguageService.CurrentLanguage);
            }
        }


        [JSInvokable]
        public async Task HandleSpeechResult(string transcript)
        {
            CurrentMessage = transcript;
            Console.WriteLine($"Speech result: {CurrentMessage}");
            StateHasChanged();

            if (!string.IsNullOrWhiteSpace(CurrentMessage))
            {
                await SendMessage();
            }
        }

        [JSInvokable]
        public void HandleSpeechError(string error)
        {
            Console.Error.WriteLine($"Speech recognition error: {error}");
            isListening = false;
            StateHasChanged();
        }

        [JSInvokable]
        public void HandleSpeechEnd()
        {
            isListening = false;
            StateHasChanged();
        }

        private async Task ToggleListenAsync()
        {
            if (isListening)
            {
                await LLMInteropService.StopSpeechRecognitionAsync();
                isListening = false;
                StateHasChanged();
            }
            else
            {
                isListening = true;
                StateHasChanged();

                // Now, start the background process.
                var langCode = LanguageService.CurrentLanguage == "ru" ? "ru-RU" : "en-US";
                await LLMInteropService.StartSpeechRecognitionAsync(langCode);
            }
        }

        public void Dispose()
        {
            objRef?.Dispose();
            Mediator.ConversationSelected -= HandleConversationSelected;
            Mediator.NewConversationRequested -= HandleNewConversationRequest;
        }
    }
}