using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ProfileTests(KitaphaneApiFactory factory)
{
    private async Task<(HttpClient Client, Session Session)> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        var session = await new AuthApi(client, factory.Emails).CreateAccountAsync(name: "Profil Sahibi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (client, session);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Get_profile_returns_the_current_user()
    {
        var (client, session) = await SignedInClientAsync();

        var response = await client.GetAsync("/api/profile");
        var profile = await JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Profil Sahibi", profile.GetProperty("name").GetString());
        Assert.Equal(session.Email, profile.GetProperty("email").GetString());
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("profileImage").ValueKind);
    }

    [Fact]
    public async Task Update_profile_changes_name_and_image_but_not_email()
    {
        var (client, session) = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/profile", new
        {
            name = "  Yeni Ad  ",
            profileImage = "https://example.com/me.png",
            email = "baska@example.com",
        });
        var profile = await JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Yeni Ad", profile.GetProperty("name").GetString());
        Assert.Equal("https://example.com/me.png", profile.GetProperty("profileImage").GetString());
        Assert.Equal(session.Email, profile.GetProperty("email").GetString());

        var me = await JsonAsync(await client.GetAsync("/api/auth/me"));
        Assert.Equal("Yeni Ad", me.GetProperty("name").GetString());
        Assert.Equal("https://example.com/me.png", me.GetProperty("profileImage").GetString());
    }

    [Fact]
    public async Task Empty_profile_image_removes_the_image()
    {
        var (client, _) = await SignedInClientAsync();
        await client.PutAsJsonAsync("/api/profile", new { name = "Ad", profileImage = "https://example.com/me.png" });

        var profile = await JsonAsync(await client.PutAsJsonAsync("/api/profile", new { name = "Ad", profileImage = "  " }));

        Assert.Equal(JsonValueKind.Null, profile.GetProperty("profileImage").ValueKind);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("Ad", "ftp://example.com/me.png")]
    [InlineData("Ad", "resim")]
    public async Task Invalid_profile_is_rejected(string name, string? profileImage)
    {
        var (client, _) = await SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/profile", new { name, profileImage });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Profile_requires_authentication()
    {
        var client = factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/api/profile", new { name = "X", profileImage = (string?)null })).StatusCode);
    }
}
