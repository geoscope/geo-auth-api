using System.Threading.Tasks;
using geo_auth_data.models.dto;

namespace geo_auth_api.Interfaces
{
    public interface IUserService
    {
        Task<AuthenticateResponse> LoginAsync(AuthenticateRequest model);

        Task<bool> LogoutAsync(LogoutRequest model);

        Task<User> GetByIdAsync(long id);
    }
}
