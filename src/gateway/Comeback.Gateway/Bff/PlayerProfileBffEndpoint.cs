namespace Comeback.Gateway.Bff;

using System.Net;
using System.Net.Http.Json;

public static class PlayerProfileBffEndpoint
{
    public static async Task<IResult> Handle(
        Guid userId,
        IHttpClientFactory httpClientFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var profileClient = httpClientFactory.CreateClient("profile-internal");
        var ratingClient = httpClientFactory.CreateClient("rating-internal");

        ForwardAuthHeader(httpContext, profileClient);
        ForwardAuthHeader(httpContext, ratingClient);

        var profileRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/profiles/{userId}");
        var ratingRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/rating/players/{userId}");

        var profileTask = profileClient.SendAsync(profileRequest, ct);
        var ratingTask = ratingClient.SendAsync(ratingRequest, ct);

        await Task.WhenAll(profileTask, ratingTask);

        var profileResponse = await profileTask;

        if (profileResponse.StatusCode == HttpStatusCode.NotFound)
            return Results.NotFound();

        if (!profileResponse.IsSuccessStatusCode)
            return Results.StatusCode((int)profileResponse.StatusCode);

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileData>(cancellationToken: ct);

        PlayerXpData? rating = null;
        var ratingResponse = await ratingTask;
        if (ratingResponse.IsSuccessStatusCode)
            rating = await ratingResponse.Content.ReadFromJsonAsync<PlayerXpData>(cancellationToken: ct);

        return Results.Ok(new PlayerProfileBffResponse(profile!, rating));
    }

    private static void ForwardAuthHeader(HttpContext httpContext, HttpClient client)
    {
        if (httpContext.Request.Headers.TryGetValue("Authorization", out var value))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", value.ToString());
    }
}
