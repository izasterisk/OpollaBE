using BLL.DTOs.Authentication;

namespace BLL.Interfaces.Infrastructure;

public interface IProfileApi
{
    Task<ProfileResponseDTO> GetProfileAsync(string token, CancellationToken cancellationToken = default);
}
