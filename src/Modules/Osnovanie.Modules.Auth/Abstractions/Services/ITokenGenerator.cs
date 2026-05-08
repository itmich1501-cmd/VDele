using CSharpFunctionalExtensions;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Abstractions.Services;

public interface ITokenGenerator
{
    Result<string, Errors> GenerateToken(User user);
}