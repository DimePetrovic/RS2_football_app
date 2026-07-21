namespace Comeback.Rating.Api.Endpoints.Rating;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class RatingEndpoints
{
    public static IEndpointRouteBuilder MapRatingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rating").WithTags("Rating");

        group.MapGet("/players/{userId:guid}", GetPlayerXpEndpoint.Handle)
            .WithName("GetPlayerXp");

        return app;
    }
}
