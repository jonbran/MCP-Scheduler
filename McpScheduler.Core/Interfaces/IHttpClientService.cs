using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace McpScheduler.Core.Interfaces
{
    /// <summary>
    /// Interface for HTTP client operations
    /// </summary>
    public interface IHttpClientService
    {
        /// <summary>
        /// Sends an HTTP request to the specified endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint URL</param>
        /// <param name="method">The HTTP method</param>
        /// <param name="content">The content to send</param>
        /// <param name="headers">Optional headers to include</param>
        /// <returns>True if the request was successful, false otherwise</returns>
        Task<bool> SendRequestAsync(string endpoint, HttpMethod method, string content, System.Collections.Generic.IDictionary<string, string>? headers = null);
    }
}
