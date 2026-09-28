using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Hubs;


public class BookingHubDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var bearerAuth = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" },
        };

        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = "Realtime (SignalR)",
        });

        var eventSchema = context.SchemaGenerator.GenerateSchema(typeof(BookingChangedEvent), context.SchemaRepository);

        swaggerDoc.Paths.Add($"{Route}/negotiate", new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = [new OpenApiTag { Name = "Realtime (SignalR)" }],
                    // Summary = "SignalR negotiate (real Try-it-out: Authorize with a JWT first)",
                    Parameters =
                    [
                        new OpenApiParameter
                        {
                            Name = "negotiateVersion",
                            In = ParameterLocation.Query,
                            Required = true,
                            Schema = new OpenApiSchema { Type = "integer", Default = new Microsoft.OpenApi.Any.OpenApiInteger(1) },
                        },
                    ],
                    Security = [new OpenApiSecurityRequirement { [bearerAuth] = [] }],
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Connection data: connectionId, connectionToken, availableTransports. " +
                                "Then open a WebSocket to " + Route + " carrying the connection token.",
                        },
                        ["401"] = new OpenApiResponse { Description = "Missing or invalid JWT." },
                    },
                },
            },
        });

        swaggerDoc.Paths.Add(Route, new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = [new OpenApiTag { Name = "Realtime (SignalR)" }],
                    // Summary = "WebSocket endpoint (descriptive: connect with a WS client, not Swagger UI)",
                    Description = "Open a WebSocket to this path with `?access_token=<JWT>`. " +
                        "Incoming server-to-client method: `BookingChanged`.",
                    Security = [new OpenApiSecurityRequirement { [bearerAuth] = [] }],
                    Responses = new OpenApiResponses
                    {
                        ["101"] = new OpenApiResponse
                        {
                            Description = "Protocol switch. Pushed payload per event:",
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType { Schema = eventSchema },
                            },
                        },
                        ["401"] = new OpenApiResponse { Description = "Missing or invalid JWT." },
                    },
                },
            },
        });
    }

    private static string Route => BookingHub.Route;
}
