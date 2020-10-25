using System.Threading.Tasks;
using geo_auth_data.models.dto;

namespace geo_auth_api.Interfaces
{
    public interface ICachingService
    {
        Task CreateUserSession(long ownerId, string userName, string token);

        Task ExtendUserSession(long ownerId, string userName, string token);

        Task GetCurrentUserSession(long ownerId, string userName);

        Task DestroyUserSession(long ownerId, string userName);
    }
}