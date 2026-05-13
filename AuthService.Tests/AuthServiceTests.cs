using FluentAssertions;
using Moq;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Auth;
using Shared.Common.DTOs.Common;
using Shared.Common.Exceptions;
using Shared.Common.Interfaces;
using Shared.Common.Models;
using Shared.Common.Validators;
using Shared.Common.Validators.Auth;
using Shared.Infrastructure.Security;
using Xunit;

namespace AuthService.Tests;

// ============================================================
// Unit Tests - Auth Service
// Pattern: Arrange-Act-Assert
// ============================================================

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public async Task Register_ValidRequest_ShouldPassValidation()
    {
        var request = new RegisterRequest("testuser", "test@email.com", "P@ssw0rd123", "P@ssw0rd123");
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "test@email.com", "P@ssw0rd123", "P@ssw0rd123")]      // Empty username
    [InlineData("ab", "test@email.com", "P@ssw0rd123", "P@ssw0rd123")]    // Short username
    [InlineData("test", "invalid-email", "P@ssw0rd123", "P@ssw0rd123")]   // Invalid email
    [InlineData("test", "test@email.com", "weak", "weak")]                 // Weak password
    [InlineData("test", "test@email.com", "P@ssw0rd123", "Different1!")]   // Mismatch
    public async Task Register_InvalidRequest_ShouldFailValidation(
        string username, string email, string password, string confirm)
    {
        var request = new RegisterRequest(username, email, password, confirm);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("admin'; DROP TABLE Users;--")]
    [InlineData("SELECT * FROM Users")]
    [InlineData("user; DELETE FROM")]
    public async Task Register_SqlInjectionAttempt_ShouldFail(string username)
    {
        var request = new RegisterRequest(username, "test@email.com", "P@ssw0rd123", "P@ssw0rd123");
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnDifferentHashForSamePassword()
    {
        var (hash1, _) = PasswordHasher.HashPassword("TestPassword123!");
        var (hash2, _) = PasswordHasher.HashPassword("TestPassword123!");
        hash1.Should().NotBe(hash2); // Different salts
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        var (hash, _) = PasswordHasher.HashPassword("TestPassword123!");
        PasswordHasher.VerifyPassword("TestPassword123!", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        var (hash, _) = PasswordHasher.HashPassword("TestPassword123!");
        PasswordHasher.VerifyPassword("WrongPassword!", hash).Should().BeFalse();
    }
}

public class InputSanitizerTests
{
    [Theory]
    [InlineData("<script>alert('xss')</script>", "&lt;alert(&#39;xss&#39;)")]
    [InlineData("javascript:alert(1)", "alert(1)")]
    [InlineData("normal text", "normal text")]
    public void SanitizeHtml_ShouldRemoveXss(string input, string expectedContains)
    {
        var result = InputSanitizer.SanitizeHtml(input);
        result.Should().NotContain("<script");
        result.Should().NotContain("javascript:");
    }

    [Fact]
    public void SanitizeHtml_NullInput_ShouldReturnNull()
    {
        InputSanitizer.SanitizeHtml(null!).Should().BeNull();
    }
}

public class PaginationTests
{
    [Fact]
    public void PaginatedResponse_ShouldCalculateTotalPages()
    {
        var response = new PaginatedResponse<string>
        {
            Items = new List<string> { "a", "b" },
            TotalCount = 25,
            PageNumber = 1,
            PageSize = 10
        };

        response.TotalPages.Should().Be(3);
        response.HasPrevious.Should().BeFalse();
        response.HasNext.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResponse_LastPage_ShouldNotHaveNext()
    {
        var response = new PaginatedResponse<string>
        {
            TotalCount = 25,
            PageNumber = 3,
            PageSize = 10
        };

        response.HasNext.Should().BeFalse();
        response.HasPrevious.Should().BeTrue();
    }
}

public class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_ShouldSetCorrectProperties()
    {
        var result = ApiResponse<string>.SuccessResponse("data", "OK");
        result.Success.Should().BeTrue();
        result.Data.Should().Be("data");
        result.Message.Should().Be("OK");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void FailResponse_ShouldSetCorrectProperties()
    {
        var errors = new List<string> { "Error 1", "Error 2" };
        var result = ApiResponse<string>.FailResponse("Failed", errors);
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Errors.Should().HaveCount(2);
    }
}
