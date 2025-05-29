using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using McpScheduler.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace McpScheduler.Infrastructure.Services
{
    /// <summary>
    /// Implementation of the HTTP client service
    /// </summary>
    public class HttpClientService : IHttpClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpClientService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientService"/> class
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory</param>
        /// <param name="logger">The logger</param>
        public HttpClientService(IHttpClientFactory httpClientFactory, ILogger<HttpClientService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> SendRequestAsync(string endpoint, HttpMethod method, string content, IDictionary<string, string>? headers = null)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException(nameof(endpoint));

            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(method, endpoint);

                // Add content if provided
                if (!string.IsNullOrEmpty(content))
                {
                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                }

                // Add headers if provided
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Send the request
                var response = await client.SendAsync(request);

                // Log the result
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("HTTP request to {Endpoint} completed successfully with status {StatusCode}",
                        endpoint, (int)response.StatusCode);
                }
                else
                {
                    _logger.LogWarning("HTTP request to {Endpoint} failed with status {StatusCode}",
                        endpoint, (int)response.StatusCode);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending HTTP request to {Endpoint}", endpoint);
                return false;
            }
        }
    }
}
