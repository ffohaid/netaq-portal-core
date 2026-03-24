using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Netaq.Application.Common.Models;
using Netaq.Domain.Enums;
using Netaq.Domain.Interfaces;

namespace Netaq.Application.Auth.Commands;

// --- DTOs ---
public record LoginRequest(string Email, string Password);
public record LoginResponse(bool RequiresOtp, string? AccessToken, string? RefreshToken, string? Message);
public record VerifyOtpRequest(string Email, string OtpCode);
public record RefreshTokenRequest(string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

// --- Login Command ---
public record LoginCommand(string Email, string Password) : IRequest<ApiResponse<LoginResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResponse>>
{
    private readonly IApplicationDbContext _context;

    public LoginCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Bypass tenant filter for login (RLS bypass for SSO/Auth)
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

        if (user == null || user.Status != UserStatus.Active)
        {
            return ApiResponse<LoginResponse>.Failure("Invalid credentials or account is not active.");
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash!, user.PasswordSalt!))
        {
            return ApiResponse<LoginResponse>.Failure("Invalid credentials.");
        }

        // Check if OTP is required
        if (user.Organization.IsOtpEnabled)
        {
            // Generate and store OTP
            var otpCode = GenerateOtp();
            user.OtpCode = otpCode;
            user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<LoginResponse>.Success(new LoginResponse(
                RequiresOtp: true,
                AccessToken: null,
                RefreshToken: null,
                Message: "OTP sent to your email."
            ));
        }

        // No OTP required - return tokens directly (handled in controller)
        return ApiResponse<LoginResponse>.Success(new LoginResponse(
            RequiresOtp: false,
            AccessToken: null, // Will be populated by controller
            RefreshToken: null,
            Message: "Login successful."
        ));
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var hmac = new HMACSHA512(saltBytes);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(computedHash) == storedHash;
    }

    private static string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes) % 1000000;
        return number.ToString("D6");
    }
}

// --- Verify OTP Command ---
public record VerifyOtpCommand(string Email, string OtpCode) : IRequest<ApiResponse<bool>>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.OtpCode).NotEmpty().Length(6);
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public VerifyOtpCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

        if (user == null)
            return ApiResponse<bool>.Failure("User not found.");

        if (user.OtpCode != request.OtpCode)
            return ApiResponse<bool>.Failure("Invalid OTP code.");

        if (user.OtpExpiresAt < DateTime.UtcNow)
            return ApiResponse<bool>.Failure("OTP code has expired.");

        // Clear OTP
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true);
    }
}
