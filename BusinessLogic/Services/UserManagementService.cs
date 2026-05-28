using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<UserManagementService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserManagementDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapUser).ToList();
    }

    public async Task<UserManagementDto?> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : MapUser(user);
    }

    public async Task<UserOperationResult> CreateUserAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationMessage = await ValidateCreateAsync(request, cancellationToken);
            if (validationMessage is not null)
            {
                return new UserOperationResult(false, validationMessage);
            }

            var user = await _userRepository.AddAsync(
                new User
                {
                    FullName = request.FullName.Trim(),
                    Email = request.Email.Trim(),
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    Role = UserRoleNames.Normalize(request.Role),
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);

            return new UserOperationResult(true, "Tạo tài khoản thành công.", user.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Create user failed for email {Email}", request.Email);
            return new UserOperationResult(false, "Có lỗi khi tạo tài khoản.");
        }
    }

    public async Task<OperationResult> UpdateUserAsync(
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return new OperationResult(false, "Không tìm thấy tài khoản.");
            }

            var validationMessage = await ValidateUpdateAsync(request, cancellationToken);
            if (validationMessage is not null)
            {
                return new OperationResult(false, validationMessage);
            }

            user.FullName = request.FullName.Trim();
            user.Email = request.Email.Trim();
            user.Role = UserRoleNames.Normalize(request.Role);
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return new OperationResult(true, "Cập nhật tài khoản thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Update user failed for user {UserId}", request.UserId);
            return new OperationResult(false, "Có lỗi khi cập nhật tài khoản.");
        }
    }

    public async Task<OperationResult> ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.NewPassword.Length < 8)
            {
                return new OperationResult(false, "Mật khẩu mới phải có ít nhất 8 ký tự.");
            }

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return new OperationResult(false, "Không tìm thấy tài khoản.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            return new OperationResult(true, "Đặt lại mật khẩu thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Reset password failed for user {UserId}", request.UserId);
            return new OperationResult(false, "Có lỗi khi đặt lại mật khẩu.");
        }
    }

    private async Task<string?> ValidateCreateAsync(
        CreateUserRequestDto request,
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

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            return "Email đã được sử dụng.";
        }

        if (!UserRoleNames.IsValid(request.Role))
        {
            return "Vai trò không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return "Mật khẩu phải có ít nhất 8 ký tự.";
        }

        return null;
    }

    private async Task<string?> ValidateUpdateAsync(
        UpdateUserRequestDto request,
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

        if (!UserRoleNames.IsValid(request.Role))
        {
            return "Vai trò không hợp lệ.";
        }

        if (request.UserId == request.CurrentAdminUserId && !request.IsActive)
        {
            return "Không thể khoá chính tài khoản đang đăng nhập.";
        }

        return null;
    }

    private static UserManagementDto MapUser(User user)
    {
        return new UserManagementDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
