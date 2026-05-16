using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Osnovanie.Api.Configuration;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT-токен. Вставлять без префикса \"Bearer \" — Swagger UI добавит сам."
        };

        var bearerRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        if (document.Paths is null)
            return Task.CompletedTask;

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem.Operations is null)
                continue;

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(bearerRequirement);
            }
        }

        return Task.CompletedTask;
    }
}
