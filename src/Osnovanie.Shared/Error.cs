namespace Osnovanie.Shared;

public record Error
{
    private const string SEPARATOR = "||";
    
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public string? InvalidField { get; }

    private Error(string code, string message, ErrorType type, string? invalidField = null)
    {
        Code = code;
        Message = message;
        Type = type;
        InvalidField = invalidField;
    }
    
    public static Error Validation(string? code, string? message, string? invalidField = null) => 
        new(code, message, ErrorType.VALIDATION, invalidField: null);
    
    public static Error Failure(string code, string message) => new(code, message, ErrorType.FAILURE);
    
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NOT_FOUND);
    
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.CONFLICT);
    
    public static Error Authentification(string code, string message) => new(code, message, ErrorType.AUTHENTICATION);
    
    public static Error Authorization(string code, string message) => new(code, message, ErrorType.AUTHORIZATION);

    public string Serialize()
    {
        return string.Join(SEPARATOR, Code, Message, Type);
    }

    public static Error Deserialize(string searialazed)
    {
        string[] parts = searialazed.Split(SEPARATOR);

        if (parts.Length < 3)
        {
            throw new ArgumentException("Invalid serialized format");
        }

        if (Enum.TryParse<ErrorType>(parts[2], out var type) == false)
        {
            throw new ArgumentException("Invalid serialized format");
        }
        
        return new Error(parts[0], parts[1], type);
    }

    public Errors ToErrors() => new([this]);
}

public enum ErrorType
{
    NOT_FOUND,
    FAILURE,
    VALIDATION,
    CONFLICT,
    AUTHENTICATION,
    AUTHORIZATION,
}