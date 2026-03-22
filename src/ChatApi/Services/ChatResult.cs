using RetrievalService;

namespace ChatApi.Services;

internal sealed record ChatResult(
    string Message,
    string[] Tags,
    IReadOnlyList<RetrievalResult> Sources);
