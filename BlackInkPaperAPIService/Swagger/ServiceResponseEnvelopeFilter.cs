using Common.YourProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlackInkPaperAPIService.Swagger;

/// <summary>
/// Restates every documented response as the shape the API actually puts on the wire.
///
/// Controllers annotate the payload type — <c>[ProducesResponseType&lt;OrderDto&gt;]</c> — but
/// <see cref="Controllers.Extensions.ControllerResponseExtensions.ToApiResult{T}"/> never sends a
/// bare payload. Success is wrapped in <see cref="ServiceResponse{T}"/> and failure is a
/// <see cref="ProblemDetails"/>, so the annotations describe <c>data</c> rather than the body and
/// any generated client deserialises the wrong thing at the top level.
///
/// Doing this as a filter rather than by rewriting ~89 annotations keeps the envelope in one
/// place: a new endpoint is documented correctly because it goes through the same extension
/// method, not because someone remembered the generic wrapper.
/// </summary>
public sealed class ServiceResponseEnvelopeFilter : IOperationFilter
{
    /// <summary>204 carries no body, so there is nothing to wrap.</summary>
    private const int NoContent = StatusCodes.Status204NoContent;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var declared in context.ApiDescription.SupportedResponseTypes)
        {
            if (declared.StatusCode == NoContent) continue;
            if (!operation.Responses.TryGetValue(declared.StatusCode.ToString(), out var response)) continue;

            var schema = IsSuccess(declared.StatusCode)
                ? EnvelopeSchema(declared.Type, context)
                : Schema(typeof(ProblemDetails), context);

            ApplySchema(response, schema);
        }
    }

    private static bool IsSuccess(int statusCode) => statusCode is >= 200 and < 300;

    /// <summary>
    /// Builds the schema for <c>ServiceResponse&lt;T&gt;</c>. Generating the closed generic (rather
    /// than hand-writing properties) keeps the documented envelope honest if a field is ever added
    /// to <see cref="ServiceResponse{T}"/>.
    /// </summary>
    private static OpenApiSchema EnvelopeSchema(Type? payloadType, OperationFilterContext context)
    {
        // An action with no declared payload type still returns the envelope — it just carries no
        // meaningful data, so document it with an unconstrained one rather than inventing a type.
        var payload = payloadType is null || payloadType == typeof(void)
            ? typeof(object)
            : payloadType;

        return Schema(typeof(ServiceResponse<>).MakeGenericType(payload), context);
    }

    private static OpenApiSchema Schema(Type type, OperationFilterContext context)
        => context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

    /// <summary>
    /// Replaces the schema on every content type already negotiated for the response, and seeds
    /// <c>application/json</c> when the response was declared without one — an untyped
    /// <c>[ProducesResponseType(404)]</c> still returns a ProblemDetails body.
    /// </summary>
    private static void ApplySchema(OpenApiResponse response, OpenApiSchema schema)
    {
        if (response.Content.Count == 0)
        {
            response.Content["application/json"] = new OpenApiMediaType { Schema = schema };
            return;
        }

        foreach (var media in response.Content.Values)
            media.Schema = schema;
    }
}
