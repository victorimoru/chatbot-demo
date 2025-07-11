using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WorkerAssistant.Client;
using WorkerAssistant.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ILLMInteropService, LLMInteropService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IVectorStoreService, VectorStoreService>();

await builder.Build().RunAsync();
