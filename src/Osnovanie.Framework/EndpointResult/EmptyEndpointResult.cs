using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Osnovanie.Shared;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Osnovanie.Framework.EndpointResult;

public sealed class EndpointResult : IResult
{
    private readonly IResult _result;

    public EndpointResult(UnitResult<Error> result)
    {
        _result = result.IsSuccess
            ? new SuccessResult<EmptyResponse>(new EmptyResponse("OK"))
            : new ErrorsResult(result.Error);
    }

    public EndpointResult(UnitResult<Errors> result)
    {
        _result = result.IsSuccess
            ? new SuccessResult<EmptyResponse>(new EmptyResponse("OK"))
            : new ErrorsResult(result.Error);
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        return _result.ExecuteAsync(httpContext);
    }
    
    public static implicit operator EndpointResult(UnitResult<Error> result)
        => new(result);

    public static implicit operator EndpointResult(UnitResult<Errors> result)
        => new(result);
}