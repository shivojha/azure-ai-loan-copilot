using Azure.AI.OpenAI;
using ChatApi.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;

namespace ChatApi.Services;

internal sealed class AzureOpenAiChatResponder(
    IOptions<AzureOpenAiOptions> options,
    ILogger<AzureOpenAiChatResponder> logger) : IChatResponder
{
    private readonly AzureOpenAiOptions _options = options.Value;
    private readonly ILogger<AzureOpenAiChatResponder> _logger = logger;

    public async Task<ChatResult> GetReplyAsync(string message, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return _options.UseMockFallback
                ? CreateMockFallback(message, "azure-config-missing")
                : throw new InvalidOperationException(
                    "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint, AzureOpenAI:Deployment, and AzureOpenAI:ApiKey.");
        }

        try
        {
            AzureOpenAIClient azureClient = new(
                new Uri(_options.Endpoint),
                new ApiKeyCredential(_options.ApiKey));

            ChatClient chatClient = azureClient.GetChatClient(_options.Deployment);

            List<ChatMessage> messages =
            [
                new SystemChatMessage(_options.SystemPrompt),
                new UserChatMessage(message)
            ];

            ChatCompletionOptions chatOptions = new()
            {
                Temperature = _options.Temperature
            };

            ClientResult<ChatCompletion> completion = await chatClient.CompleteChatAsync(
                messages,
                chatOptions,
                cancellationToken);

            string reply = completion.Value.Content[0].Text;

            return new ChatResult(reply, ["azure-openai", "step-3"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI chat completion failed.");

            if (_options.UseMockFallback)
            {
                return CreateMockFallback(message, "azure-request-failed");
            }

            throw;
        }
    }

    private static ChatResult CreateMockFallback(string message, string reason)
    {
        string reply = global::MockLoanAssistant.GenerateReply(message);
        return new ChatResult(reply, ["mock-fallback", reason, "step-3"]);
    }
}
