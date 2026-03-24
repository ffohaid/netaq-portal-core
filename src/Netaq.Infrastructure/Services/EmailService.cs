using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Netaq.Infrastructure.Services;

/// <summary>
/// Email service for sending invitations, OTP codes, and notifications.
/// SMTP configuration loaded from environment variables (never hardcoded).
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendInvitationAsync(string to, string invitationToken, string orgName, CancellationToken cancellationToken = default);
    Task SendOtpAsync(string to, string otpCode, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(string smtpHost, int smtpPort, string smtpUser, string smtpPassword, string fromEmail, string fromName)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPassword = smtpPassword;
        _fromEmail = fromEmail;
        _fromName = fromName;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        
        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(_smtpUser, _smtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendInvitationAsync(string to, string invitationToken, string orgName, CancellationToken cancellationToken = default)
    {
        var subject = $"دعوة للانضمام إلى منصة نِطاق - {orgName} | Invitation to NETAQ Portal - {orgName}";
        var htmlBody = $@"
        <div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px;'>
            <h2>مرحباً بك في منصة نِطاق</h2>
            <p>تمت دعوتك للانضمام إلى منصة نِطاق الخاصة بـ {orgName}.</p>
            <p>يرجى استخدام الرابط التالي لإكمال التسجيل:</p>
            <a href='{{BASE_URL}}/auth/accept-invitation?token={invitationToken}' 
               style='background-color: #1a56db; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; margin: 16px 0;'>
                قبول الدعوة
            </a>
            <hr/>
            <h2 dir='ltr'>Welcome to NETAQ Portal</h2>
            <p dir='ltr'>You have been invited to join NETAQ Portal for {orgName}.</p>
            <p dir='ltr'>Please use the following link to complete your registration:</p>
            <a href='{{BASE_URL}}/auth/accept-invitation?token={invitationToken}' 
               style='background-color: #1a56db; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; margin: 16px 0;'>
                Accept Invitation
            </a>
        </div>";
        
        await SendAsync(to, subject, htmlBody, cancellationToken);
    }

    public async Task SendOtpAsync(string to, string otpCode, CancellationToken cancellationToken = default)
    {
        var subject = "رمز التحقق - منصة نِطاق | Verification Code - NETAQ Portal";
        var htmlBody = $@"
        <div dir='rtl' style='font-family: Arial, sans-serif; padding: 20px;'>
            <h2>رمز التحقق</h2>
            <p>رمز التحقق الخاص بك هو:</p>
            <div style='font-size: 32px; font-weight: bold; color: #1a56db; padding: 16px; background: #f0f4ff; border-radius: 8px; text-align: center; letter-spacing: 8px;'>
                {otpCode}
            </div>
            <p>صالح لمدة 5 دقائق فقط.</p>
            <hr/>
            <h2 dir='ltr'>Verification Code</h2>
            <p dir='ltr'>Your verification code is:</p>
            <div style='font-size: 32px; font-weight: bold; color: #1a56db; padding: 16px; background: #f0f4ff; border-radius: 8px; text-align: center; letter-spacing: 8px;'>
                {otpCode}
            </div>
            <p dir='ltr'>Valid for 5 minutes only.</p>
        </div>";
        
        await SendAsync(to, subject, htmlBody, cancellationToken);
    }
}
