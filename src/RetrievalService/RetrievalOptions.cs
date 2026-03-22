namespace RetrievalService;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    /// <summary>Path to the knowledge base directory containing .md files.</summary>
    public string KnowledgeBasePath { get; set; } = "data/loan-kb";

    /// <summary>Maximum number of chunks to return before applying token budget.</summary>
    public int MaxResults { get; set; } = 3;

    /// <summary>Approximate token budget for all retrieved snippets combined.</summary>
    public int MaxRetrievalTokens { get; set; } = 2000;

    /// <summary>Minimum relevance score (0–1) a chunk must reach to be included.</summary>
    public float MinRelevanceScore { get; set; } = 0.1f;
}
