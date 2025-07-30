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
builder.Services.AddSingleton<IWebSpeechTtsService, WebSpeechTtsService>();


// In Program.cs
builder.Services.AddLocalization();

var host =  builder.Build();

// --- NEW: Read the cookie and set the culture ---
var languageService = host.Services.GetRequiredService<ILanguageService>();
await ((LanguageService)languageService).InitializeAsync();

var culture = new CultureInfo(languageService.CurrentLanguage);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();