using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

/// <summary>Reading goals: CRUD, user isolation and progress computed from books.</summary>
[Collection(ApiCollection.Name)]
public sealed class ReadingGoalsTests(KitaphaneApiFactory factory)
{
    private const string Year2026Start = "2026-01-01";
    private const string Year2026End = "2026-12-31";

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        var session = await new AuthApi(client, factory.Emails).CreateAccountAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<JsonElement> CreateGoalAsync(
        HttpClient client, string type = "BOOK_COUNT", int target = 10, string startDate = Year2026Start, string endDate = Year2026End)
    {
        var response = await client.PostAsJsonAsync("/api/reading-goals", new { type, target, startDate, endDate });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await JsonAsync(response);
    }

    private static async Task CreateBookAsync(
        HttpClient client, string status, int pageCount, int currentPage = 0, string? startDate = null, string? finishDate = null)
    {
        var response = await client.PostAsJsonAsync("/api/books", new
        {
            title = $"Kitap {Guid.NewGuid():N}",
            author = "Yazar",
            pageCount,
            currentPage,
            status,
            isbn = (string?)null,
            rating = (int?)null,
            startDate,
            finishDate,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Created_goal_is_listed_with_its_progress()
    {
        var client = await SignedInClientAsync();

        var goal = await CreateGoalAsync(client, "PAGE_COUNT", 10000);

        Assert.Equal("PAGE_COUNT", goal.GetProperty("type").GetString());
        Assert.Equal(10000, goal.GetProperty("target").GetInt32());
        Assert.Equal(Year2026Start, goal.GetProperty("startDate").GetString());
        Assert.Equal(Year2026End, goal.GetProperty("endDate").GetString());
        Assert.Equal(0, goal.GetProperty("current").GetInt32());
        Assert.Equal(0, goal.GetProperty("percentage").GetInt32());

        var list = await JsonAsync(await client.GetAsync("/api/reading-goals"));
        Assert.Equal(goal.GetProperty("id").GetString(), Assert.Single(list.EnumerateArray()).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Book_count_progress_counts_books_completed_in_the_goal_range()
    {
        var client = await SignedInClientAsync();
        await CreateBookAsync(client, "COMPLETED", 100, startDate: "2025-12-01", finishDate: "2026-01-01"); // first day
        await CreateBookAsync(client, "COMPLETED", 100, startDate: "2026-03-01", finishDate: "2026-12-31"); // last day
        await CreateBookAsync(client, "COMPLETED", 100, startDate: "2025-11-01", finishDate: "2025-12-31"); // before
        await CreateBookAsync(client, "READING", 100, currentPage: 50, startDate: "2026-02-01");           // not completed

        var goal = await CreateGoalAsync(client, "BOOK_COUNT", 3);

        Assert.Equal(2, goal.GetProperty("current").GetInt32());
        Assert.Equal(66, goal.GetProperty("percentage").GetInt32()); // rounded down
    }

    [Fact]
    public async Task Page_count_progress_uses_the_pages_read_formula_and_may_exceed_the_target()
    {
        var client = await SignedInClientAsync();
        await CreateBookAsync(client, "COMPLETED", 300, startDate: "2026-01-10", finishDate: "2026-02-10");
        await CreateBookAsync(client, "READING", 400, currentPage: 120, startDate: "2026-05-01");
        await CreateBookAsync(client, "READING", 400, currentPage: 90, startDate: "2025-12-15"); // started before
        await CreateBookAsync(client, "TO_READ", 500);

        var goal = await CreateGoalAsync(client, "PAGE_COUNT", 400);

        Assert.Equal(420, goal.GetProperty("current").GetInt32());
        Assert.Equal(105, goal.GetProperty("percentage").GetInt32());
    }

    [Fact]
    public async Task Overlapping_goals_are_allowed_and_listed_latest_start_first()
    {
        var client = await SignedInClientAsync();
        await CreateGoalAsync(client, "BOOK_COUNT", 30, "2026-01-01", "2026-12-31");
        await CreateGoalAsync(client, "PAGE_COUNT", 5000, "2026-06-01", "2026-08-31");
        await CreateGoalAsync(client, "BOOK_COUNT", 5, "2025-01-01", "2026-06-30");

        var list = await JsonAsync(await client.GetAsync("/api/reading-goals"));

        Assert.Equal(
            ["2026-06-01", "2026-01-01", "2025-01-01"],
            list.EnumerateArray().Select(goal => goal.GetProperty("startDate").GetString()));
    }

    [Fact]
    public async Task Goal_can_be_updated_and_deleted()
    {
        var client = await SignedInClientAsync();
        var id = (await CreateGoalAsync(client)).GetProperty("id").GetString();

        var update = await client.PutAsJsonAsync($"/api/reading-goals/{id}", new
        {
            type = "PAGE_COUNT",
            target = 2500,
            startDate = "2026-03-01",
            endDate = "2026-03-31",
        });
        var updated = await JsonAsync(update);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("PAGE_COUNT", updated.GetProperty("type").GetString());
        Assert.Equal(2500, updated.GetProperty("target").GetInt32());
        Assert.Equal("2026-03-31", updated.GetProperty("endDate").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/reading-goals/{id}")).StatusCode);
        Assert.Empty((await JsonAsync(await client.GetAsync("/api/reading-goals"))).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/reading-goals/{id}")).StatusCode);
    }

    [Fact]
    public async Task Other_users_goals_are_not_visible_or_editable()
    {
        var owner = await SignedInClientAsync();
        var other = await SignedInClientAsync();
        var id = (await CreateGoalAsync(owner)).GetProperty("id").GetString();

        Assert.Empty((await JsonAsync(await other.GetAsync("/api/reading-goals"))).EnumerateArray());

        var update = await other.PutAsJsonAsync($"/api/reading-goals/{id}", new
        {
            type = "BOOK_COUNT",
            target = 1,
            startDate = Year2026Start,
            endDate = Year2026End,
        });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal("READING_GOAL_NOT_FOUND", await AuthApi.ReadErrorCodeAsync(update));
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/reading-goals/{id}")).StatusCode);

        Assert.Single((await JsonAsync(await owner.GetAsync("/api/reading-goals"))).EnumerateArray());
    }

    public static TheoryData<object> InvalidGoals => new()
    {
        new { type = "BOOK_COUNT", target = 0, startDate = Year2026Start, endDate = Year2026End },
        new { type = "BOOK_COUNT", target = -5, startDate = Year2026Start, endDate = Year2026End },
        new { type = "BOOK_COUNT", target = 1_000_001, startDate = Year2026Start, endDate = Year2026End },
        new { type = "BOOK_COUNT", target = 10, startDate = "2026-05-01", endDate = "2026-05-01" },
        new { type = "BOOK_COUNT", target = 10, startDate = "2026-05-01", endDate = "2026-04-30" },
        new { type = "BOOK_COUNT", target = 10, startDate = (string?)null, endDate = Year2026End },
        new { type = "BOOK_COUNT", target = 10, startDate = Year2026Start, endDate = (string?)null },
        new { type = "MINUTES", target = 10, startDate = Year2026Start, endDate = Year2026End },
    };

    [Theory]
    [MemberData(nameof(InvalidGoals))]
    public async Task Invalid_goal_is_rejected(object body)
    {
        var client = await SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/reading-goals", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Reading_goals_require_authentication()
    {
        var client = factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reading-goals")).StatusCode);
    }
}
