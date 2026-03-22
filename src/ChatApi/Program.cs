using ChatApi.Configuration;
using ChatApi.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using RetrievalService;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<AzureOpenAiOptions>(
    builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));
builder.Services.Configure<RetrievalOptions>(
    builder.Configuration.GetSection(RetrievalOptions.SectionName));

builder.Services.AddSingleton<IRetrievalService>(sp =>
    new LocalFileRetriever(sp.GetRequiredService<IOptions<RetrievalOptions>>().Value));
builder.Services.AddSingleton<IChatResponder, AzureOpenAiChatResponder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("frontend");

var loanKbPath = Path.Combine(AppContext.BaseDirectory, "data/loan-kb");
if (Directory.Exists(loanKbPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(loanKbPath),
        RequestPath = "/api/docs"
    });
}

app.MapGet("/api/health", (IOptions<AzureOpenAiOptions> options) => Results.Ok(new
{
    status = "ok",
    service = "ChatApi",
    mode = options.Value.IsConfigured ? "azure-openai" : "mock-fallback",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/chat/prompts", () => Results.Ok(new[]
{
    "What documents do I need for pre-approval?",
    "How much should I budget for closing costs?",
    "What is the difference between pre-qualification and pre-approval?"
}));

app.MapPost("/api/chat", async (
    ChatRequest request,
    IChatResponder chatResponder,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message is required." });
    }

    ChatResult result = await chatResponder.GetReplyAsync(request.Message, cancellationToken);

    return Results.Ok(new ChatResponse(
        Guid.NewGuid().ToString("N"),
        "assistant",
        result.Message,
        DateTimeOffset.UtcNow,
        result.Tags,
        result.Sources.Select(s => new SourceDto(s.SourceName, s.Snippet, s.Relevance, $"/api/docs/{s.FileName}")).ToArray()));
});

app.Run();

internal static class MockLoanAssistant
{
    public static string GenerateReply(string message)
    {
        var normalized = message.Trim().ToLowerInvariant();

        if (normalized.Contains("pre-approval") || normalized.Contains("preapproval"))
        {
            return "For pre-approval, most lenders usually ask for pay stubs, W-2s, bank statements, a photo ID, and permission to review your credit. This mock fallback stays available during phase 3 in case Azure OpenAI settings are not ready yet.";
        }

        if (normalized.Contains("closing cost"))
        {
            return "Closing costs often land around 2% to 5% of the home price, depending on lender fees, taxes, insurance, and local requirements. This response came from the local fallback path while Azure OpenAI is being wired in.";
        }

        if (normalized.Contains("credit score"))
        {
            return "Credit score expectations vary by loan type, but stronger scores usually improve rate options and approval odds. This is the fallback response shape the UI will still accept if Azure OpenAI is unavailable.";
        }

        if (normalized.Contains("hello") || normalized.Contains("hi"))
        {
            return "Hi, I'm the loan assistant. Azure OpenAI is being integrated now, and the local fallback remains in place so the app stays usable during setup.";
        }

        return "This is the local fallback response. Once Azure OpenAI settings are configured, the same `/api/chat` endpoint will return model-generated answers without requiring a frontend contract change.";
    }
}

internal sealed record ChatRequest(
    [property: JsonPropertyName("message")] string Message);

internal sealed record SourceDto(
    [property: JsonPropertyName("sourceName")] string SourceName,
    [property: JsonPropertyName("snippet")] string Snippet,
    [property: JsonPropertyName("relevance")] float Relevance,
    [property: JsonPropertyName("fileUrl")] string FileUrl);

internal sealed record ChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("tags")] string[] Tags,
    [property: JsonPropertyName("sources")] SourceDto[] Sources);
