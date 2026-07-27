using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DroneFactory.Commands;

/// <summary>
/// Fills in the Swagger summary/description per route (method, relative path) from
/// <see cref="EndpointDocs"/>, since minimal API lambdas in net6.0 have no attribute-based
/// equivalent to controller XML doc comments and <c>WithOpenApi()</c> requires a package only
/// available from net7.0 onward. The actual text lives in EndpointDocs.json, not here.
/// </summary>
public sealed class EndpointDocsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? string.Empty;
        var path = context.ApiDescription.RelativePath ?? string.Empty;

        if (!EndpointDocs.Operations.TryGetValue($"{method} /{path}", out var doc))
        {
            return;
        }

        operation.Summary = doc.Summary;
        if (!string.IsNullOrEmpty(doc.Description))
        {
            operation.Description = doc.Description;
        }
    }
}
