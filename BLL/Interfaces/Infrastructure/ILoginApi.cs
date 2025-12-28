namespace BLL.Interfaces.Infrastructure;

public interface ILoginApi
{
    Task<string> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
