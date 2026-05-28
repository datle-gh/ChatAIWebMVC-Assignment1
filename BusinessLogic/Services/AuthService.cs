using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResultDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResultDto(false, null, "Vui lòng nhập email và mật khẩu.");
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResultDto(false, null, "Email hoặc mật khẩu không đúng.");
            }

            if (!user.IsActive)
            {
                return new AuthResultDto(false, null, "Tài khoản đã bị khoá.");
            }

            return new AuthResultDto(true, MapUser(user), "Đăng nhập thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Login failed for email {Email}", request.Email);
            return new AuthResultDto(false, null, "Có lỗi khi đăng nhập.");
        }
    }

    private static AuthUserDto MapUser(User user)
    {
        return new AuthUserDto(user.Id, user.FullName, user.Email, user.Role);
    }
}
