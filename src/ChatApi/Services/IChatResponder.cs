namespace ChatApi.Services;

internal interface IChatResponder
{
    Task<ChatResult> GetReplyAsync(string message, CancellationToken cancellationToken);
}
