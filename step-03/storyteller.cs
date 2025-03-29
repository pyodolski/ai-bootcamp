using System.ClientModel;

using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

using OpenAI;

var config = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddUserSecrets<Program>()
                 .Build();

var builder = Kernel.CreateBuilder();
if (string.IsNullOrWhiteSpace(config["Azure:OpenAI:Endpoint"]!) == false)
{
    var client = new AzureOpenAIClient(
        new Uri(config["Azure:OpenAI:Endpoint"]!),
        new AzureKeyCredential(config["Azure:OpenAI:ApiKey"]!));

    builder.AddAzureOpenAIChatCompletion(
                deploymentName: config["Azure:OpenAI:DeploymentName"]!,
                azureOpenAIClient: client);
}
else
{
    var client = new OpenAIClient(
        credential: new ApiKeyCredential(config["GitHub:Models:AccessToken"]!),
        options: new OpenAIClientOptions { Endpoint = new Uri(config["GitHub:Models:Endpoint"]!) });

    builder.AddOpenAIChatCompletion(
                modelId: config["GitHub:Models:ModelId"]!,
                openAIClient: client);
}
var kernel = builder.Build();
var definition = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../..", "Plugins", "StoryTellerAgent", "StoryTeller.yaml"));
var template = KernelFunctionYaml.ToPromptTemplateConfig(definition);
var agent = new ChatCompletionAgent(template, new KernelPromptTemplateFactory())
            {
                Kernel = kernel
            };
var history = new ChatHistory();
history.AddSystemMessage("You're a very good storyteller agent. Always answer in Korean.");
var input = default(string);
var message = default(string);
while (true)
{
    Console.WriteLine("Hi, I'm your friendly storyteller. What story would you like me to tell you about?");
    Console.Write("User: ");
    input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    Console.Write("Assistant: ");
    history.AddUserMessage(input);
    var arguments = new KernelArguments()
    {
        { "topic", input },
        { "length", 3 }
    };

    var response = agent.InvokeStreamingAsync(history, arguments);
    await foreach (var content in response)
    {
        await Task.Delay(20);
        message += content;
        Console.Write(content);
    }
    history.AddAssistantMessage(message!);
    Console.WriteLine();

    Console.WriteLine();
}

