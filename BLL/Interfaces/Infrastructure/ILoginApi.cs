namespace BLL.Interfaces.Infrastructure;

public interface ILoginApi
{
    Task<object> LoginAsync(string username, string password);
}
