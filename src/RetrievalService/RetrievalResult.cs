namespace RetrievalService;

public sealed record RetrievalResult(
    string SourceName,
    string Snippet,
    float Relevance,
    string FileName);
