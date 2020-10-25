using System.Collections.Generic;
using System.Threading.Tasks;
using geo_auth_data.models.domain;
using geo_auth_data.models.dto;

namespace geo_auth_api.Interfaces
{
    public interface IUserService
    {
        Task<AuthenticateResponse> LoginAsync(AuthenticateRequest model);

        Task<bool> LogoutAsync(LogoutRequest model);

        Task<geo_auth_data.models.dto.User> GetByIdAsync(long id);

        Task<IEnumerable<Role>> GetUserRolesAsync(long userId);
    }
}
