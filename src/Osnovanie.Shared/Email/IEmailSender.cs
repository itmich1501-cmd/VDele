using CSharpFunctionalExtensions;

namespace Osnovanie.Shared.Email;

public interface IEmailSender
{
    Task<UnitResult<Errors>> SendAsync(MailData mailData);
}