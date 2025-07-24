using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using WorkerAssistant.Client.Resources;
using WorkerAssistant.Client.Services;

namespace WorkerAssistant.Client.Pages
{
    public partial class ChatApp : IDisposable
    {
        [Inject] private ILLMInteropService LLMInteropService { get; set; } = default!;
        [Inject] private IVectorStoreService VectorStoreService { get; set; } = default!;
        [Inject] private ILanguageService LanguageService { get; set; } = default!;
        [Inject] private IStringLocalizer<AppStrings> Localizer { get; set; } = default!;
        [Inject] private IConversationMediator Mediator { get; set; } = default!;

        private DotNetObjectReference<ChatApp> _objRef;

        private bool isOverlayVisible = false;
        private string overlayText = "Preparing Assistant...";
        private bool isInitialized = false;
        private int initializationProgress = 0;

        protected override void OnInitialized()
        {
            Mediator.InitializationRequested += OnInitializationRequested;
        }

        private async void OnInitializationRequested()
        {
            await StartInitializationAsync();
        }

        private async Task StartInitializationAsync()
        {
            isOverlayVisible = true;
            StateHasChanged();

            _objRef = DotNetObjectReference.Create(this);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

            try
            {
                // --- Step 1: Check Cache Status ---
                overlayText = Localizer["CheckingStatus"];
                await InvokeAsync(StateHasChanged);
                try
                {
                    var status = await LLMInteropService.CheckModelCacheStatusAsync("Qwen2-1.5B-Instruct-q4f16_1-MLC");
                    overlayText = (status?.Progress == 1)
                        ? Localizer["LoadingFromCache"]
                        : Localizer["DownloadingEngine"];
                }
                catch (JSException)
                {
                    Console.WriteLine("Cache check failed, proceeding with full download.");
                    overlayText = Localizer["DownloadingEngine"];
                }
                await InvokeAsync(StateHasChanged);

                // --- Step 2: Initialize the LLM Engine (with progress) ---
                await LLMInteropService.InitializeEngineAsync(_objRef, cts.Token);

                // --- Step 3: Build the Knowledge Index ---
                overlayText = Localizer["BuildingIndex"];

                await InvokeAsync(StateHasChanged);
                var langCode = LanguageService.CurrentLanguage;
                var knowledgeBaseFile = $"improved_knowledge_base_{langCode}.json";
                await VectorStoreService.BuildIndexAsync(knowledgeBaseFile);

                // --- Step 4: Success ---
                isInitialized = true;
                Mediator.NotifyInitializationCompleted();
            }
            catch (TaskCanceledException)
            {
                overlayText = Localizer["TimeoutError"];
            }
            catch (Exception ex)
            {
                overlayText = ex.Message.Contains("WebGPU is not supported")
                    ? Localizer["WebGPUError"]
                    : Localizer["GenericError"];
                Console.Error.WriteLine(ex);
            }
            finally
            {
                isOverlayVisible = !isInitialized;
                await InvokeAsync(StateHasChanged);
            }
        }

        [JSInvokable]
        public void HandleInitializationUpdate(string message, int progress)
        {
           // overlayText = message;
            overlayText = $"Downloading AI Model ({progress}%)... This can take a moment on your first visit.";
            initializationProgress = progress;
            StateHasChanged();
        }

        [JSInvokable]
        public void HandleInitializationError(string message)
        {
            overlayText = $"ERROR: {message}";
            initializationProgress = 0;
            StateHasChanged();
        }

        public void Dispose()
        {
            _objRef?.Dispose();
            Mediator.InitializationRequested -= OnInitializationRequested;
        }
    }
}