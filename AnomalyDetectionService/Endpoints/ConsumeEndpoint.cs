using Messaging;

namespace AnomalyDetectionService.Endpoints;

public static class ConsumeEndpoint
{
    
    public static void MapConsumeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");
        
        group.MapGet("/", () => "Hi Users!");
        group.MapPost("/consume", () =>
        {
            
        });
    }

    
}