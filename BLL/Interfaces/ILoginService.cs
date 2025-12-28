using BLL.DTOs.Authentication;

namespace BLL.Interfaces;

public interface ILoginService
{
    Task<ProfileResponseDTO> LoginAsync(string username, string password);
    Task LogoutAsync(string username);
}