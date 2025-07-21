using Microsoft.AspNetCore.Components;
using WorkerAssistant.Client.Services;

namespace WorkerAssistant.Client.Pages
{
    public partial class ChatApp
    {
        [Inject]
        private ILLMInteropService LLMInteropService { get; set; } = default!;

        [Inject]
        private IVectorStoreService VectorStoreService { get; set; } = default!;

        [Inject]
        private ILanguageService LanguageService { get; set; } = default!;

        private bool isOverlayVisible;
        private string overlayText = "";
        private bool isInitialized = false;
        private bool isInitializing = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (isInitializing)
                {
                    return;
                }

                isInitializing = true;
                isOverlayVisible = true;

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                var cancellationToken = cts.Token;

                try
                {
                    double statusNumber = 0;
                    overlayText = "Checking model status...";
                    try
                    {
                        await InvokeAsync(StateHasChanged); 
                        var status = await LLMInteropService.CheckModelCacheStatusAsync("Qwen2-1.5B-Instruct-q4f16_1-MLC");
                       // var status = await LLMInteropService.CheckModelCacheStatusAsync("gemma-2b-it-q4f16_1-MLC");
                        statusNumber = status?.Progress ?? 0;
                    }
                    catch (Exception)
                    {
                        // If the cache check fails (e.g., race condition), log the error and proceed.
                        Console.WriteLine($"Cache check failed, proceeding with full download:");
                        overlayText = "Downloading AI Engine (this may take a moment)...";
                    }
                   
                    overlayText = (statusNumber == 1)
                       ? "Loading AI Engine from cache..."
                       : "Downloading AI Engine (this may take a moment)...";

                    await InvokeAsync(StateHasChanged);

                    try
                    {
                        // --- Step 1: Initialize the LLM Engine ---
                        // StateHasChanged is implicitly called by Blazor after an await,
                        // so the UI will update with the new text.
                        await LLMInteropService.InitializeEngineAsync(cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        overlayText = "The process took too long and has timed out. Please check your internet connection and try again.";
                    }

                    // --- Step 2: Build the Knowledge Index ---
                    overlayText = "Building knowledge Base....";

                    await InvokeAsync(StateHasChanged);
                    var langCode = LanguageService.CurrentLanguage;
                    var knowledgeBaseFile = $"improved_knowledge_base_{langCode}.json";
                    Console.WriteLine("Knowledge Base File: " + knowledgeBaseFile);
                    await VectorStoreService.BuildIndexAsync(knowledgeBaseFile);

                    overlayText = "Building knowledge Base Completed...";
                    // --- Step 3: Finalize ---
                    isInitialized = true;
                    isOverlayVisible = false;
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {

                    if (ex.Message.Contains("WebGPU is not supported"))
                    {
                        overlayText = "Error: WebGPU is not supported or enabled in your browser. " +
                            "Please use a modern browser like Chrome or Edge and ensure WebGPU is enabled.";
                    }
                    // Critical: Handle any errors during initialization.
                    // In a real app, you would log the full exception here.
                    // The overlay remains visible to show the error message.
                    Console.WriteLine(ex); // Log to console for debugging
                }
                finally
                {
                    if (isInitialized)
                    {
                        isOverlayVisible = false;
                    }

                    isInitializing = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
    }
}