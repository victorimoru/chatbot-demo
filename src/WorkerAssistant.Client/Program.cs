using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using WorkerAssistant.Client;
using WorkerAssistant.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ILLMInteropService, LLMInteropService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IVectorStoreService, VectorStoreService>();
builder.Services.AddScoped<IConversationMediator, ConversationMediator>();
builder.Services.AddSingleton<ILanguageService, LanguageService>();


// In Program.cs
builder.Services.AddLocalization();

var host =  builder.Build();

var supportedCultures = new[] { "en", "ru" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Set the default culture for the application's threads
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en");

await host.RunAsync();