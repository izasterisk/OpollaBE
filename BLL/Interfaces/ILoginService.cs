namespace BLL.Interfaces;

public interface ILoginService
{
    Task<object> LoginAsync(string username, string password);
}