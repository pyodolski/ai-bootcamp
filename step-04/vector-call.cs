using System.ClientModel;

using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

using Workshop.ConsoleApp.Services;

using OpenAI;

var config = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddUserSecrets<Program>()
                 .Build();

var builder = Kernel.CreateBuilder();
builder.AddGoogleAIGeminiChatCompletion(
        modelId: config["Google:Gemini:ModelIds:ChatCompletion"]!,
        apiKey: config["Google:Gemini:ApiKey"]!
    );
var kernel = builder.Build();
var service = new TextSearchService(config);
var collection = await service.GetVectorStoreRecordCollectionAsync("records");
var search = await service.GetVectorStoreTextSearchAsync(collection);
var settings = new PromptExecutionSettings()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
var input = default(string);
var message = default(string);
while (true)
{
    Console.WriteLine("Ask a question about semantic kernel.");
    Console.Write("User: ");
    input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    Console.Write("Assistant: ");

    var searchResponse = await search.GetTextSearchResultsAsync(input, new TextSearchOptions() { Top = 2, Skip = 0 });
    Console.WriteLine("\n--- Text Search Results ---\n");
    await foreach (var result in searchResponse.Results)
    {
        Console.WriteLine($"Name:  {result.Name}");
        Console.WriteLine($"Value: {result.Value}");
        Console.WriteLine($"Link:  {result.Link}");
        Console.WriteLine();
    }
    var functionCallingArguments = new KernelArguments(settings);

    var functionCalingResponse = kernel.InvokePromptStreamingAsync(
        promptTemplate: input,
        arguments: functionCallingArguments);

    Console.WriteLine("\n--- Text Search Results from Chat Completions with Auto Function Calling ---\n");
    await foreach (var content in functionCalingResponse)
    {
        await Task.Delay(20);
        message += content;
        Console.Write(content);
    }
    Console.WriteLine();
    Console.WriteLine();

    Console.WriteLine();
}

