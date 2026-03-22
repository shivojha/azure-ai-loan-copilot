namespace RetrievalService;

public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievalResult>> QueryAsync(string question, CancellationToken cancellationToken);
}
