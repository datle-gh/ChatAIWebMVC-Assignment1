using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<AccountService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AccountProfileDto?> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : MapProfile(user);
    }

    public async Task<OperationResult> UpdateProfileAsync(
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return new OperationResult(false, "Không tìm thấy tài khoản.");
            }

            if (!user.IsActive)
            {
                return new OperationResult(false, "Tài khoản đã bị khoá.");
            }

            var validationMessage = await ValidateProfileAsync(request, cancellationToken);
            if (validationMessage is not null)
            {
                return new OperationResult(false, validationMessage);
            }

            user.FullName = request.FullName.Trim();
            user.Email = request.Email.Trim();

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            return new OperationResult(true, "Cập nhật tài khoản thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Profile update failed for user {UserId}", request.UserId);
            return new OperationResult(false, "Có lỗi khi cập nhật tài khoản.");
        }
    }

    private async Task<string?> ValidateProfileAsync(
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return "Vui lòng nhập họ tên.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Vui lòng nhập email.";
        }

        if (await _userRepository.EmailExistsAsync(request.Email, request.UserId, cancellationToken))
        {
            return "Email đã được sử dụng.";
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return "Vui lòng nhập mật khẩu hiện tại.";
            }

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null || !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return "Mật khẩu hiện tại không đúng.";
            }

            if (request.NewPassword.Length < 8)
            {
                return "Mật khẩu mới phải có ít nhất 8 ký tự.";
            }
        }

        return null;
    }

    private static AccountProfileDto MapProfile(User user)
    {
        return new AccountProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
