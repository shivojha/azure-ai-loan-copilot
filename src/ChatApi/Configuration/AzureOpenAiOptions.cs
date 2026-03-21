namespace ChatApi.Configuration;

internal sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; init; } = string.Empty;

    public string Deployment { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SystemPrompt { get; init; } =
        "You are a helpful mortgage and loan assistant. Give concise, practical guidance, note when rules vary by lender or location, and avoid pretending to know borrower-specific facts you were not given.";

    public int MaxOutputTokens { get; init; } = 400;

    public float Temperature { get; init; } = 0.4f;

    public bool UseMockFallback { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(Deployment)
        && !string.IsNullOrWhiteSpace(ApiKey);
}
