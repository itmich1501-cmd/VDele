using Microsoft.AspNetCore.Routing;

namespace Osnovanie.Framework.EndpointSettings;

public interface IEndpoint
{ 
    void MapEndpoint(IEndpointRouteBuilder app);
}