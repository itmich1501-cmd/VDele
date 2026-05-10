namespace Osnovanie.Modules.Auth.Configuration;

public class SmsIntOptions
{
    public const string SECTION_NAME = "SmsInt";

    public string BaseUrl { get; set; } = "https://lcab.smsint.ru/json/v1.0/";
    public string Token { get; set; } = string.Empty;
}