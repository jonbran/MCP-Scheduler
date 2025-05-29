using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Api.Controllers
{
    /// <summary>
    /// Controller for authentication operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class
        /// </summary>
        /// <param name="jwtTokenService">The JWT token service</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="logger">The logger</param>
        public AuthController(IJwtTokenService jwtTokenService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token for the specified client
        /// </summary>
        /// <param name="request">The token request</param>
        /// <returns>The token response</returns>
        [HttpPost("token")]
        [AllowAnonymous]
        public ActionResult<TokenResponse> GenerateToken([FromBody] TokenRequest request)
        {
            // In a real-world application, validate client credentials against a database
            // For demo purposes, we'll use a simple API key check
            if (string.IsNullOrEmpty(request.ClientId) || request.ApiKey != _configuration["Auth:ApiKey"])
            {
                _logger.LogWarning("Invalid authentication attempt for client {ClientId}", request.ClientId);
                return Unauthorized();
            }

            _logger.LogInformation("Generating token for client {ClientId}", request.ClientId);
            var token = _jwtTokenService.GenerateToken(request.ClientId);

            return new TokenResponse
            {
                Token = token,
                ExpiresIn = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60") * 60 // Convert minutes to seconds
            };
        }

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        /// <param name="request">The validation request</param>
        /// <returns>The validation response</returns>
        [HttpPost("validate")]
        [AllowAnonymous]
        public ActionResult<TokenValidationResponse> ValidateToken([FromBody] TokenValidationRequest request)
        {
            var clientId = _jwtTokenService.ValidateToken(request.Token);

            if (string.IsNullOrEmpty(clientId))
            {
                return Ok(new TokenValidationResponse { IsValid = false });
            }

            return Ok(new TokenValidationResponse
            {
                IsValid = true,
                ClientId = clientId
            });
        }

        /// <summary>
        /// Token request model
        /// </summary>
        public class TokenRequest
        {
            /// <summary>
            /// The client ID
            /// </summary>
            public required string ClientId { get; set; }

            /// <summary>
            /// The API key
            /// </summary>
            public required string ApiKey { get; set; }
        }

        /// <summary>
        /// Token response model
        /// </summary>
        public class TokenResponse
        {
            /// <summary>
            /// The JWT token
            /// </summary>
            public required string Token { get; set; }

            /// <summary>
            /// Token expiry in seconds
            /// </summary>
            public int ExpiresIn { get; set; }
        }

        /// <summary>
        /// Token validation request model
        /// </summary>
        public class TokenValidationRequest
        {
            /// <summary>
            /// The token to validate
            /// </summary>
            public required string Token { get; set; }
        }

        /// <summary>
        /// Token validation response model
        /// </summary>
        public class TokenValidationResponse
        {
            /// <summary>
            /// Whether the token is valid
            /// </summary>
            public bool IsValid { get; set; }

            /// <summary>
            /// The client ID from the token if valid
            /// </summary>
            public string? ClientId { get; set; }
        }
    }
}
