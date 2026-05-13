namespace Osnovanie.Modules.Auth.Configuration;

public class PhoneVerificationOptions
{
    public int CodeLifetimeSeconds { get; set; }
    public List<string> TestPhones { get; set; } = new();
    public string TestPhoneFixedCode { get; set; } = string.Empty;
}