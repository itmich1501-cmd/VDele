using CSharpFunctionalExtensions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Osnovanie.Shared;
using Osnovanie.Shared.Email;

namespace Osnovanie.Infrastructure.Email;

public class EmailSender : IEmailSender
{
    private readonly MailOptions _mailOptions;

    public EmailSender(IOptions<MailOptions> options)
    {
        _mailOptions = options.Value;
    }

    public async Task<UnitResult<Errors>> SendAsync(MailData mailData)
    {
        var mail = new MimeMessage();

        mail.From.Add(new MailboxAddress(_mailOptions.FromDisplayName, _mailOptions.From));

        var tryParse = MailboxAddress.TryParse(mailData.Email, out var mailAddress);
        if (!tryParse)
        {
            return UnitResult.Failure<Errors>(Error.Failure("email.invalid", "Некорректный email адрес"));
        }

        mail.To.Add(mailAddress!);

        var body = new BodyBuilder
        {
            HtmlBody = mailData.Body
        };

        mail.Body = body.ToMessageBody();
        mail.Subject = mailData.Subject;

        using var client = new SmtpClient();
        
        await client.ConnectAsync(_mailOptions.Host, _mailOptions.Port);
        await client.AuthenticateAsync(_mailOptions.UserName, _mailOptions.Password);
        await client.SendAsync(mail);

        return UnitResult.Success<Errors>();
    }
}