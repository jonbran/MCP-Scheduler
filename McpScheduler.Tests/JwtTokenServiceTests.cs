using System;
using System.Collections.Generic;
using McpScheduler.Core.Interfaces;
using McpScheduler.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace McpScheduler.Tests
{
    public class JwtTokenServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _mockJwtSection;
        private readonly string _testKey = "ThisIsAVeryLongSecretKeyUsedForTestingPurposesOnly1234567890";
        private readonly string _testIssuer = "TestIssuer";
        private readonly string _testAudience = "TestAudience";
        private readonly string _testExpiryMinutes = "60";

        public JwtTokenServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockJwtSection = new Mock<IConfigurationSection>();

            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns(_testKey);
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns(_testIssuer);
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns(_testAudience);
            _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns(_testExpiryMinutes);
        }

        [Fact]
        public void GenerateToken_ShouldReturnValidToken()
        {
            // Arrange
            var clientId = "test-client";
            var service = new JwtTokenService(_mockConfiguration.Object);

            // Act
            var token = service.GenerateToken(clientId);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnClientId()
        {
            // Arrange
            var clientId = "test-client";
            var service = new JwtTokenService(_mockConfiguration.Object);
            var token = service.GenerateToken(clientId);

            // Act
            var result = service.ValidateToken(token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(clientId, result);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var service = new JwtTokenService(_mockConfiguration.Object);

            // Act
            var result = service.ValidateToken("invalid.token.string");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GenerateToken_WithAdditionalClaims_ShouldReturnValidToken()
        {
            // Arrange
            var clientId = "test-client";
            var additionalClaims = new Dictionary<string, string>
            {
                { "role", "admin" },
                { "custom", "value" }
            };
            var service = new JwtTokenService(_mockConfiguration.Object);

            // Act
            var token = service.GenerateToken(clientId, additionalClaims);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }
    }
}
