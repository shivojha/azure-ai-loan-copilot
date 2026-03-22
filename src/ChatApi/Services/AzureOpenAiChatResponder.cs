using Azure.AI.OpenAI;
using ChatApi.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using RetrievalService;
using System.ClientModel;

namespace ChatApi.Services;

internal sealed class AzureOpenAiChatResponder(
    IOptions<AzureOpenAiOptions> options,
    IRetrievalService retrieval,
    ILogger<AzureOpenAiChatResponder> logger) : IChatResponder
{
    private readonly AzureOpenAiOptions _options = options.Value;

    // Reuse the client and chat client across requests — avoids creating a new
    // HTTP connection pool on every query.
    private readonly Lazy<ChatClient?> _chatClient = new(() =>
    {
        var o = options.Value;
        if (!o.IsConfigured) return null;
        return new AzureOpenAIClient(new Uri(o.Endpoint), new ApiKeyCredential(o.ApiKey))
            .GetChatClient(o.Deployment);
    });

    public async Task<ChatResult> GetReplyAsync(string message, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return _options.UseMockFallback
                ? CreateMockFallback(message, "azure-config-missing")
                : throw new InvalidOperationException(
                    "Azure OpenAI is not configured. Set AzureOpenAI:Endpoint, AzureOpenAI:Deployment, and AzureOpenAI:ApiKey.");
        }

        IReadOnlyList<RetrievalResult> sources = [];
        string retrievalTag = "retrieval-miss";

        try
        {
            sources = await retrieval.QueryAsync(message, cancellationToken);
            retrievalTag = sources.Count > 0 ? "with-retrieval" : "retrieval-miss";
            logger.LogInformation("Retrieval returned {Count} chunk(s) for query.", sources.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Retrieval failed; continuing without context.");
            retrievalTag = "retrieval-error";
        }

        try
        {
            ChatClient chatClient = _chatClient.Value!;

            string systemPrompt = BuildSystemPrompt(_options.SystemPrompt, sources);

            List<ChatMessage> messages =
            [
                new SystemChatMessage(systemPrompt),
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

            return new ChatResult(reply, ["azure-openai", retrievalTag, "step-4a"], sources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure OpenAI chat completion failed.");

            if (_options.UseMockFallback)
                return CreateMockFallback(message, "azure-request-failed");

            throw;
        }
    }

    private static string BuildSystemPrompt(string basePrompt, IReadOnlyList<RetrievalResult> sources)
    {
        if (sources.Count == 0)
            return basePrompt;

        var contextBlock = string.Join("\n\n", sources.Select(s =>
            $"[{s.SourceName}]\n{s.Snippet}"));

        return $"{basePrompt}\n\nUse the following loan knowledge to answer accurately:\n\n{contextBlock}\n\nIf the provided context does not address the question, answer from your general knowledge and say so.";
    }

    private static ChatResult CreateMockFallback(string message, string reason)
    {
        string reply = global::MockLoanAssistant.GenerateReply(message);
        return new ChatResult(reply, ["mock-fallback", reason, "step-4a"], []);
    }
}
