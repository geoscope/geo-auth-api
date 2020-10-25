using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using geo_auth_api.Interfaces;
using geo_auth_data.Interfaces;
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

        IHydrator<geo_auth_data.models.domain.User, geo_auth_data.models.dto.User> userToUserDtoHydrator;

        public UserService(IOptions<AppSettings> appSettings, IUserRepository userRepository, IHydrator<geo_auth_data.models.domain.User, geo_auth_data.models.dto.User> userToUserDtoHydrator,
            ICachingService cachingService)
        {
            this.appSettings = appSettings.Value;
            this.userRepository = userRepository;
            this.userToUserDtoHydrator = userToUserDtoHydrator;
            this.cachingService = cachingService;
        }

        public async Task<AuthenticateResponse> LoginAsync(AuthenticateRequest model)
        {
            var user = await userRepository.AuthenticateUserAsync(model.OwnerId, model.Username, model.Password);

            if (user == null)
                return null;

            // authentication successful so generate jwt token
            var userDto = userToUserDtoHydrator.Hydrate(user);
            var token = generateJwtToken(userDto);

            await cachingService.CreateUserSession(model.OwnerId, userDto.UserName, token);

            return new AuthenticateResponse(userDto, token);
        }

        public async Task<User> GetByIdAsync(long id)
        {
            var user = await userRepository.GetSingleAsync(id);
            var userDto = userToUserDtoHydrator.Hydrate(user);

            return userDto;
        }

        private string generateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appSettings.JwtSharedSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim("id", user.Id.ToString()),
                    new Claim("username", user.UserName),
                    new Claim("email", user.Email),
                    new Claim("firstname", user.FirstName),
                    new Claim("lastname", user.LastName)
                }),
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
    }
}