using System.Globalization;
using System.Net;
using Kitaphane.Application.Auth;
using Kitaphane.Application.Common;

namespace Kitaphane.Infrastructure.Email;

/// <summary>
/// Transactional email content in Turkish (default) or English, chosen by the request culture.
/// </summary>
public static class EmailTemplates
{
    private static bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    private static int CodeMinutes => (int)AuthRules.CodeLifetime.TotalMinutes;

    public static EmailMessage VerificationCode(string to, string name, string code) => IsEnglish
        ? Build(to, "Kitaphane verification code", name,
            $"Use the code below to complete your Kitaphane sign-up. It is valid for {CodeMinutes} minutes.",
            code, "If you did not sign up for Kitaphane, you can ignore this email.", "Hello")
        : Build(to, "Kitaphane doğrulama kodu", name,
            $"Kitaphane kaydınızı tamamlamak için aşağıdaki kodu kullanın. Kod {CodeMinutes} dakika geçerlidir.",
            code, "Kitaphane'ye kayıt olmadıysanız bu e-postayı dikkate almayın.", "Merhaba");

    public static EmailMessage PasswordResetCode(string to, string name, string code) => IsEnglish
        ? Build(to, "Kitaphane password reset code", name,
            $"Use the code below to reset your Kitaphane password. It is valid for {CodeMinutes} minutes.",
            code, "If you did not request a password reset, you can ignore this email.", "Hello")
        : Build(to, "Kitaphane şifre sıfırlama kodu", name,
            $"Kitaphane şifrenizi sıfırlamak için aşağıdaki kodu kullanın. Kod {CodeMinutes} dakika geçerlidir.",
            code, "Şifre sıfırlama talebinde bulunmadıysanız bu e-postayı dikkate almayın.", "Merhaba");

    private static EmailMessage Build(
        string to, string subject, string name, string intro, string code, string footer, string greeting)
    {
        var text = $"{greeting} {name},\n\n{intro}\n\n{code}\n\n{footer}\n\nKitaphane";

        var safeName = WebUtility.HtmlEncode(name);
        var html = $"""
            <div style="font-family:Arial,sans-serif;color:#181821;max-width:480px;margin:0 auto;padding:24px">
              <h2 style="margin:0 0 16px">Kitaphane</h2>
              <p>{greeting} {safeName},</p>
              <p>{WebUtility.HtmlEncode(intro)}</p>
              <p style="font-size:32px;font-weight:bold;letter-spacing:8px;margin:24px 0;color:#009FFF">{code}</p>
              <p style="color:#8E8E93;font-size:13px">{WebUtility.HtmlEncode(footer)}</p>
            </div>
            """;

        return new EmailMessage(to, subject, text, html);
    }
}
