namespace BLL.Interfaces.Infrastructure;

public interface IProfileApi
{
    Task<object> GetProfileAsync(string token);
}
