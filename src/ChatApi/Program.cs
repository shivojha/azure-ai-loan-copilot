using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "ChatApi",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/chat/prompts", () => Results.Ok(new[]
{
    "What documents do I need for pre-approval?",
    "How much should I budget for closing costs?",
    "What is the difference between pre-qualification and pre-approval?"
}));

app.MapPost("/api/chat", (ChatRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message is required." });
    }

    var reply = MockLoanAssistant.GenerateReply(request.Message);

    return Results.Ok(new ChatResponse(
        Guid.NewGuid().ToString("N"),
        "assistant",
        reply,
        DateTimeOffset.UtcNow,
        new[]
        {
            "mock-response",
            "step-1"
        }));
});

app.Run();

internal static class MockLoanAssistant
{
    public static string GenerateReply(string message)
    {
        var normalized = message.Trim().ToLowerInvariant();

        if (normalized.Contains("pre-approval") || normalized.Contains("preapproval"))
        {
            return "For pre-approval, most lenders usually ask for pay stubs, W-2s, bank statements, a photo ID, and permission to review your credit. This is a mock answer for step 1, but it gives us a realistic shape for the UI.";
        }

        if (normalized.Contains("closing cost"))
        {
            return "Closing costs often land around 2% to 5% of the home price, depending on lender fees, taxes, insurance, and local requirements. In the mock API, we can use answers like this to exercise the chat flow before Azure OpenAI is plugged in.";
        }

        if (normalized.Contains("credit score"))
        {
            return "Credit score expectations vary by loan type, but stronger scores usually improve rate options and approval odds. This mock backend is intentionally deterministic so we can test the app without external dependencies.";
        }

        if (normalized.Contains("hello") || normalized.Contains("hi"))
        {
            return "Hi, I'm the mock loan assistant. Ask me about pre-approval, closing costs, credit, or next steps, and I'll return a canned response from the local API.";
        }

        return "This is a mock loan assistant response from the local API. Step 1 is only about proving the backend contract and chat experience, so the reply is intentionally canned until we wire in Azure OpenAI later.";
    }
}

internal sealed record ChatRequest(
    [property: JsonPropertyName("message")] string Message);

internal sealed record ChatResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("tags")] string[] Tags);
