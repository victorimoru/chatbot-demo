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

        private bool isOverlayVisible;
        private string overlayText = "";
        private bool isInitialized = false;
        private bool isInitializing = false;

        protected override async Task OnInitializedAsync()
        {
            if (isInitializing)
            {
                return;
            }

            isInitializing = true;
            isOverlayVisible = true;

            try
            {
                // --- Step 1: Initialize the LLM Engine ---
                overlayText = "Downloading Local LLM...";
                // StateHasChanged is implicitly called by Blazor after an await,
                // so the UI will update with the new text.
                await LLMInteropService.InitializeEngineAsync();

                // --- Step 2: Build the Knowledge Index ---
                overlayText = "Building knowledge index...";

                await InvokeAsync(StateHasChanged);
                await VectorStoreService.BuildIndexAsync("improved_knowledge_base.json");

                // --- Step 3: Finalize ---
                isInitialized = true;
            }
            catch (Exception ex)
            {
                // Critical: Handle any errors during initialization.
                overlayText = $"An error occurred during startup: {ex.Message}";
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
            }
        }
    }
}