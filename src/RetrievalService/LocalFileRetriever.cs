namespace RetrievalService;

public sealed class LocalFileRetriever : IRetrievalService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "for", "are", "but", "not", "you", "all", "can",
        "was", "one", "our", "out", "get", "has", "how", "its", "may", "new", "now",
        "see", "who", "did", "what", "this", "that", "with", "have", "from", "they",
        "will", "been", "when", "were", "your", "each", "she", "use", "does", "their",
        "which", "there", "about", "would", "make", "like", "into", "than", "then",
        "some", "also", "more", "other", "these", "those", "such", "most", "do", "is",
        "if", "of", "to", "in", "it", "be", "as", "at", "so", "we", "he", "by", "on"
    };

    // Chunks are pre-computed once at construction time — not on every query.
    private readonly IReadOnlyList<PrecomputedChunk> _chunks;
    private readonly RetrievalOptions _options;

    public LocalFileRetriever(RetrievalOptions options)
    {
        _options = options;
        _chunks = LoadChunks(options.KnowledgeBasePath);
    }

    public Task<IReadOnlyList<RetrievalResult>> QueryAsync(
        string question,
        CancellationToken cancellationToken)
    {
        var queryTerms = Tokenize(question);
        if (queryTerms.Count == 0)
            return Task.FromResult<IReadOnlyList<RetrievalResult>>([]);

        var scored = _chunks
            .Select(c => (Chunk: c, Score: Score(queryTerms, c.Terms)))
            .Where(x => x.Score >= _options.MinRelevanceScore)
            .OrderByDescending(x => x.Score)
            .Take(_options.MaxResults);

        var results = new List<RetrievalResult>();
        int tokensUsed = 0;

        foreach (var (chunk, score) in scored)
        {
            int estimatedTokens = chunk.Text.Length / 4;
            if (tokensUsed + estimatedTokens > _options.MaxRetrievalTokens) break;

            results.Add(new RetrievalResult(chunk.SourceName, chunk.Text, score, chunk.FileName));
            tokensUsed += estimatedTokens;
        }

        return Task.FromResult<IReadOnlyList<RetrievalResult>>(results);
    }

    // Pre-tokenized chunk — terms are computed once, reused on every query.
    private sealed record PrecomputedChunk(string SourceName, string Text, HashSet<string> Terms, string FileName);

    private static IReadOnlyList<PrecomputedChunk> LoadChunks(string knowledgeBasePath)
    {
        var kbPath = Path.IsPathRooted(knowledgeBasePath)
            ? knowledgeBasePath
            : Path.Combine(AppContext.BaseDirectory, knowledgeBasePath);

        if (!Directory.Exists(kbPath))
            return [];

        return Directory
            .GetFiles(kbPath, "*.md", SearchOption.TopDirectoryOnly)
            .SelectMany(file => ChunkMarkdown(file, File.ReadAllText(file)))
            .Select(c => new PrecomputedChunk(c.SourceName, c.Text, Tokenize(c.Text), c.FileName))
            .ToList();
    }

    private static IEnumerable<(string SourceName, string Text, string FileName)> ChunkMarkdown(
        string filePath,
        string content)
    {
        var fileName = Path.GetFileName(filePath);
        var sourceName = Path.GetFileNameWithoutExtension(filePath)
            .Replace('-', ' ')
            .Replace('_', ' ');

        string? currentHeader = null;
        var currentLines = new List<string>();

        foreach (var line in content.Split('\n'))
        {
            if (line.StartsWith("## "))
            {
                if (currentHeader is not null && currentLines.Count > 0)
                    yield return ($"{sourceName} — {currentHeader}", string.Join("\n", currentLines).Trim(), fileName);

                currentHeader = line[3..].Trim();
                currentLines = [];
            }
            else if (currentHeader is not null)
            {
                currentLines.Add(line);
            }
        }

        if (currentHeader is not null && currentLines.Count > 0)
            yield return ($"{sourceName} — {currentHeader}", string.Join("\n", currentLines).Trim(), fileName);
    }

    private static float Score(HashSet<string> queryTerms, HashSet<string> chunkTerms)
    {
        int overlap = queryTerms.Count(t => chunkTerms.Contains(t));
        return (float)overlap / queryTerms.Count;
    }

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '?', '!', ':', ';', '-', '(', ')', '"', '\'', '/'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .ToHashSet();
}
