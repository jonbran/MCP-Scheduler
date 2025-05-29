using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace McpScheduler.Core.Interfaces
{
    /// <summary>
    /// Interface for JWT token service operations
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a JWT token for the specified client ID
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="additionalClaims">Optional additional claims to include in the token</param>
        /// <returns>The generated JWT token string</returns>
        string GenerateToken(string clientId, IDictionary<string, string>? additionalClaims = null);

        /// <summary>
        /// Validates a JWT token and returns the client ID if valid
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>The client ID if token is valid, null otherwise</returns>
        string? ValidateToken(string token);
    }
}
