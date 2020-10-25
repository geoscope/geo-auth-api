using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using geo_auth_api.Interfaces;
using geo_auth_data.Interfaces;
using geo_auth_data.models.domain;
using geo_auth_data.models.dto;
using geo_auth_shared.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace geo_auth_api.Services
{
    public class UserService : IUserService
    {
        private readonly AppSettings appSettings;
        private readonly IUserRepository userRepository;
        private readonly ICachingService cachingService;
        private readonly IUserRoleRepository userRoleRepository;
        IHydrator<geo_auth_data.models.domain.User, geo_auth_data.models.dto.User> userToUserDtoHydrator;

        public UserService(IOptions<AppSettings> appSettings, IUserRepository userRepository, IHydrator<geo_auth_data.models.domain.User, geo_auth_data.models.dto.User> userToUserDtoHydrator,
            ICachingService cachingService, IUserRoleRepository userRoleRepository)
        {
            this.appSettings = appSettings.Value;
            this.userRepository = userRepository;
            this.userToUserDtoHydrator = userToUserDtoHydrator;
            this.cachingService = cachingService;
            this.userRoleRepository = userRoleRepository;
        }

        public async Task<AuthenticateResponse> LoginAsync(AuthenticateRequest model)
        {
            var user = await userRepository.AuthenticateUserAsync(model.OwnerId, model.Username, model.Password);

            if (user == null)
                return null;

            // authentication successful so generate jwt token
            var userDto = userToUserDtoHydrator.Hydrate(user);
            var token = await generateJwtToken(userDto);

            await cachingService.CreateUserSession(model.OwnerId, userDto.UserName, token);

            return new AuthenticateResponse(userDto, token);
        }

        public async Task<geo_auth_data.models.dto.User> GetByIdAsync(long id)
        {
            var user = await userRepository.GetSingleAsync(id);
            var userDto = userToUserDtoHydrator.Hydrate(user);

            return userDto;
        }

        private async Task<string> generateJwtToken(geo_auth_data.models.dto.User user)
        {
            var userRoles = await GetUserRolesAsync(user.Id);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appSettings.JwtSharedSecret);

            var claims = new List<Claim>() {
                    new Claim("id", user.Id.ToString()),
                    new Claim("username", user.UserName),
                    new Claim("email", user.Email),
                    new Claim("firstname", user.FirstName),
                    new Claim("lastname", user.LastName)
                };
            claims.AddRange(userRoles.ToList().Select(role => new Claim(ClaimTypes.Role, role.Name)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(appSettings.AuthExpiryHours),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<bool> LogoutAsync(LogoutRequest model)
        {
            var user = await userRepository.GetByUserNameAsync(model.OwnerId, model.Username);
            var userDto = userToUserDtoHydrator.Hydrate(user);

            await cachingService.DestroyUserSession(user.OwnerId, userDto.UserName);

            return true;
        }

        public Task<IEnumerable<Role>> GetUserRolesAsync(long userId)
        {
            return userRoleRepository.GetRolesByUserId(userId);
        }
    }
}