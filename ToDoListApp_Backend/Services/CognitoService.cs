using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using ToDoListApp_Backend.Models;

namespace ToDoListApp_Backend.Services
{
    public interface ICognitoService
    {
        Task<CognitoAuthResult> LoginAsync(string usernameOrPhone, string password);
        Task<CognitoAuthResult> RefreshTokenAsync(string refreshToken);
        Task<bool> SignUpAsync(string password, string name, string username, string phoneNumber);
        Task<bool> ConfirmSignUpAsync(string username, string confirmationCode);
        Task<UserInfo> GetUserInfoAsync(string accessToken);
    }
    public class CognitoService : ICognitoService
    {
        private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CognitoService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _userPoolId;

        public CognitoService(IConfiguration configuration, ILogger<CognitoService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _clientId = configuration["AWS:Cognito:ClientId"]
                ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientId");

            _clientSecret = configuration["AWS:Cognito:ClientSecret"]
                ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientSecret");

            _userPoolId = configuration["AWS:Cognito:UserPoolId"]
                ?? Environment.GetEnvironmentVariable("AWS__Cognito__UserPoolId");

            var accessKey = configuration["AWS:AccessKey"]
                ?? Environment.GetEnvironmentVariable("AWS__AccessKey");

            var secretKey = configuration["AWS:SecretKey"]
                ?? Environment.GetEnvironmentVariable("AWS__SecretKey");

            var region = configuration["AWS:Region"]
                ?? Environment.GetEnvironmentVariable("AWS__Region");

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret) || string.IsNullOrEmpty(_userPoolId))
            {
                throw new InvalidOperationException("AWS Cognito configuration is missing or incomplete");
            }

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                _cognitoClient = new AmazonCognitoIdentityProviderClient(
                    accessKey,
                    secretKey,
                    Amazon.RegionEndpoint.GetBySystemName(region)
                );
            }
            else
            {
                _cognitoClient = new AmazonCognitoIdentityProviderClient(
                    Amazon.RegionEndpoint.GetBySystemName(region)
                );
            }
        }

        public async Task<CognitoAuthResult> LoginAsync(string usernameOrPhone, string password)
        {
            try
            {
                _logger.LogInformation("Attempting to authenticate user: {UsernameOrPhone}", usernameOrPhone);

                var secretHash = ComputeSecretHash(usernameOrPhone);

                var authRequest = new AdminInitiateAuthRequest
                {
                    UserPoolId = _userPoolId,
                    ClientId = _clientId,
                    AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        {"USERNAME", usernameOrPhone},
                        {"PASSWORD", password},
                        {"SECRET_HASH", secretHash}
                    }
                };

                var response = await _cognitoClient.AdminInitiateAuthAsync(authRequest);

                _logger.LogInformation("Authentication successful for user: {UsernameOrPhone}", usernameOrPhone);
                return new CognitoAuthResult
                {
                    IsSuccess = true,
                    AccessToken = response.AuthenticationResult.AccessToken,
                    IdToken = response.AuthenticationResult.IdToken,
                    RefreshToken = response.AuthenticationResult.RefreshToken,
                    ExpiresIn = response.AuthenticationResult.ExpiresIn
                };
            }
            catch (NotAuthorizedException ex)
            {
                _logger.LogWarning("Authentication failed - Invalid credentials for user: {UsernameOrPhone} - {Message}",
                    usernameOrPhone, ex.Message);
                return new CognitoAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid username/phone or password"
                };
            }
            catch (UserNotConfirmedException ex)
            {
                _logger.LogWarning("Authentication failed - User not confirmed: {UsernameOrPhone} - {Message}",
                    usernameOrPhone, ex.Message);
                return new CognitoAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "User phone number not confirmed"
                };
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning("Authentication failed - User not found: {UsernameOrPhone} - {Message}",
                    usernameOrPhone, ex.Message);
                return new CognitoAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "User not found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for user: {UsernameOrPhone} - {Message}",
                    usernameOrPhone, ex.Message);

                // Log the full exception details to help debug SECRET_HASH issues
                _logger.LogError("Full exception details: {ExceptionType} - {FullMessage}",
                    ex.GetType().Name, ex.ToString());

                return new CognitoAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // vừa thêm secrets hash cho refresh token lúc 1h17 phút ngày 24 tháng 9 năm 2025
        public async Task<CognitoAuthResult> RefreshTokenAsync(string refreshToken)
        {
            var secretHash = ComputeSecretHashForRefresh(refreshToken);
            try
            {
                _logger.LogInformation("Attempting to refresh token");

                var request = new InitiateAuthRequest
                {
                    ClientId = _clientId,
                    AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        {"REFRESH_TOKEN", refreshToken},
                        {"SECRET_HASH",secretHash}
                    }
                };

                var response = await _cognitoClient.InitiateAuthAsync(request);

                _logger.LogInformation("Token refresh successful");
                return new CognitoAuthResult
                {
                    IsSuccess = true,
                    AccessToken = response.AuthenticationResult.AccessToken,
                    IdToken = response.AuthenticationResult.IdToken,
                    ExpiresIn = response.AuthenticationResult.ExpiresIn
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error: {Message}", ex.Message);

                _logger.LogError("Refresh token full exception: {ExceptionType} - {FullMessage}",
                    ex.GetType().Name, ex.ToString());

                return new CognitoAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> SignUpAsync(string password, string name, string username, string phoneNumber)
        {
            try
            {
                _logger.LogInformation("Attempting to sign up user: {Username} with phone: {PhoneNumber}",
                    username, phoneNumber);

                var secretHash = ComputeSecretHash(username);

                var signUpRequest = new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = username,
                    Password = password,
                    SecretHash = secretHash,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType { Name = "name", Value = name },
                        new AttributeType { Name = "phone_number", Value = phoneNumber }
                    }
                };

                var response = await _cognitoClient.SignUpAsync(signUpRequest);

                _logger.LogInformation("SignUp successful for user: {Username}, UserSub: {UserSub}",
                    username, response.UserSub);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignUp error for user: {Username} - {Message}", username, ex.Message);
                return false;
            }
        }

        public async Task<bool> ConfirmSignUpAsync(string username, string confirmationCode)
        {
            try
            {
                var secretHash = ComputeSecretHash(username);

                var confirmRequest = new ConfirmSignUpRequest
                {
                    ClientId = _clientId,
                    Username = username,
                    ConfirmationCode = confirmationCode,
                    SecretHash = secretHash
                };

                await _cognitoClient.ConfirmSignUpAsync(confirmRequest);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            try
            {
                var request = new GetUserRequest
                {
                    AccessToken = accessToken
                };

                var response = await _cognitoClient.GetUserAsync(request);

                // Extract cognito sub from JWT token
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(accessToken);
                var cognitoSub = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;

                return new UserInfo
                {
                    Username = response.Username,
                    PhoneNumber = response.UserAttributes.FirstOrDefault(x => x.Name == "phone_number")?.Value,
                    Name = response.UserAttributes.FirstOrDefault(x => x.Name == "name")?.Value,
                    CognitoSub = cognitoSub
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string ComputeSecretHash(string username)
        {
            var message = username + _clientId;
            var keyBytes = Encoding.UTF8.GetBytes(_clientSecret);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string ComputeSecretHashForRefresh(string refreshToken)
        {
            try
            {
                var message = _clientId;
                var keyBytes = Encoding.UTF8.GetBytes(_clientSecret);
                var messageBytes = Encoding.UTF8.GetBytes(message);

                using (var hmac = new HMACSHA256(keyBytes))
                {
                    var hashBytes = hmac.ComputeHash(messageBytes);
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}