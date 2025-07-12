// Using the JSDelivr CDN as it proved to be reliable.
// We import the main-thread engine creator.
import { CreateMLCEngine } from "https://cdn.jsdelivr.net/npm/@mlc-ai/web-llm/+esm";

// This variable will hold the initialized engine instance.
let engine;

/**
 * Initializes the WebLLM engine. This function is idempotent and will only
 * initialize the engine once.
 */
export async function initializeEngine() {
    if (engine) {
        console.log("Engine is already initialized.");
        return;
    }

    // The model to use. You can swap this with other model names. TinyLlama-1.1B-Chat-v1.0-q4f16_1-MLC
    // const selectedModel = "TinyLlama-1.1B-Chat-v1.0-q4f16_1-MLC";
    const selectedModel = "gemma-2b-it-q4f16_1-MLC" // "gemma-2b-it-q4f16_1-MLC"

    console.log(`Initializing engine with model: ${selectedModel}...`);

    try {
        engine = await CreateMLCEngine(
            selectedModel,
            // The progress callback is removed as it caused issues with some configurations.
        );
        console.log("Engine initialization complete.");
    } catch (err) {
        console.error("Engine initialization failed.", err);
        throw err; // Re-throw the error so C# can catch it.
    }
}

// This function checks the download status of a model without fully initializing it.
export async function checkModelCacheStatus(modelId) {
    // GetStatus is a static method on the MLCEngine class
    const status = await MLCEngine.GetStatus(modelId);
    return status;
}

/**
 * Takes a list of messages and streams the AI's response back to Blazor.
 * @param {object[]} messages The conversation history.
 * @param {object} dotnetHelper A Blazor DotNetObjectReference to call back to.
 */
export async function completeStream(messages, dotnetHelper) {
    if (!engine) {
        await dotnetHelper.invokeMethodAsync("HandleError", "Engine not initialized.");
        return;
    }

    try {
        const chunks = await engine.chat.completions.create({
            messages,
            temperature: 0.6,
            max_new_tokens: 200,
            stream: true,
            stream_options: { include_usage: true },
        });

        for await (const chunk of chunks) {
            await dotnetHelper.invokeMethodAsync("ReceiveChunkCompletion", chunk);
        }
    } catch (err) {
        await dotnetHelper.invokeMethodAsync("HandleError", `An error occurred: ${err.message}`);
    } finally {
        await dotnetHelper.invokeMethodAsync("HandleStreamCompletion");
    }
}

/**
 * Enables or disables an HTML element by its ID.
 * @param {string} elementId The ID of the element to toggle.
 * @param {boolean} isDisabled True to disable, false to enable.
 */
export function toggleElementDisabled(elementId, isDisabled) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }
    if (isDisabled) {
        element.disabled = true;
    } else {
        element.removeAttribute('disabled');
    }
}

/**
 * Triggers the highlight.js library to scan the document for new
 * code blocks and apply syntax highlighting.
 */
export function highlightCode() {
    // hljs is the global object from the highlight.js library added in index.html
    if (window.hljs) {
        window.hljs.highlightAll();
    }
}

// This will hold the embedding pipeline instance once it's ready.
let embeddingPipeline;

// This function initializes the embedding model.
export async function initializeEmbeddingModel() {

    if (embeddingPipeline) {
        console.log("Embedding pipeline is already initialized.");
        return;
    }

    // The 'pipeline' function is from Transformers.js
    // We are creating a "feature-extraction" pipeline, which is what generates embeddings.
    // "Xenova/all-MiniLM-L6-v2" is a small, fast, and very effective model for this task.-- all-MiniLM-L6-v2
    const { pipeline } = await import("https://cdn.jsdelivr.net/npm/@xenova/transformers@2.17.1");
    embeddingPipeline = await pipeline('feature-extraction', 'Xenova/bge-small-en-v1.5');

    console.log("Embedding pipeline initialized successfully.");
}

// This function takes text and returns its embedding vector.
export async function generateEmbedding(text) {

    if (!embeddingPipeline) {
        console.error("Embedding pipeline not initialized.");
        await initializeEmbeddingModel();
    }

    // Generate the embedding. The result is a Tensor object.
    const result = await embeddingPipeline(text, { pooling: 'mean', normalize: true });

    // Extract the numerical data (a Float32Array) from the tensor.
    return Array.from(result.data);
}
