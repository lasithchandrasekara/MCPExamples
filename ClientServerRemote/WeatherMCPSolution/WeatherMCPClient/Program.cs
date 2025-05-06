using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Net;

try
{
    // Retrieve OpenAI API key from environment variable
    var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrEmpty(openAiApiKey))
    {
        throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
    }

    // Create host builder
    var builder = Host.CreateEmptyApplicationBuilder(settings: null);

    // Configure logging
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

    // Add OpenAI client as a singleton
    builder.Services.AddSingleton(_ => new OpenAIClient(openAiApiKey));

    // Build the application
    var app = builder.Build();

    // Get services
    var serviceProvider = app.Services;
    var openAIClient = serviceProvider.GetRequiredService<OpenAIClient>();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    // Configure MCP client with SSE transport
    var serverUri = new Uri("http://localhost:5000");
    Console.WriteLine($"Connecting to MCP server at {serverUri}");
    
    // Create transport with SSE
    var clientTransport = new SseClientTransport(
        new SseClientTransportOptions
        {
            Endpoint = serverUri
        }
    );

    // Create client factory
    var client = await McpClientFactory.CreateAsync(clientTransport);

    // Start the application
    await app.StartAsync();

    // List available tools
    Console.WriteLine("Available tools:");
    foreach (var tool in await client.ListToolsAsync())
    {
        Console.WriteLine($"{tool.Name} ({tool.Description})");
    }

    // Sample user query
    string userQuery = "What's the weather forecast for New York City?";
    //await ProcessQueryAsync(openAIClient, client, userQuery);

    // Keep running until stopped
    await app.WaitForShutdownAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error running MCP client: {ex.Message}");
    throw;
}