using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

/// <summary>Statistics are computed from the user's books (docs/database/database-design.md §10).</summary>
[Collection(ApiCollection.Name)]
public sealed class StatisticsTests(KitaphaneApiFactory factory)
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        var session = await new AuthApi(client, factory.Emails).CreateAccountAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private static async Task CreateAsync(
        HttpClient client, string status, int pageCount, string author = "Yazar", int currentPage = 0,
        DateOnly? startDate = null, DateOnly? finishDate = null)
    {
        var response = await client.PostAsJsonAsync("/api/books", new
        {
            title = $"Kitap {Guid.NewGuid():N}",
            author,
            pageCount,
            currentPage,
            status,
            isbn = (string?)null,
            rating = (int?)null,
            startDate = startDate?.ToString("yyyy-MM-dd"),
            finishDate = finishDate?.ToString("yyyy-MM-dd"),
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Overview_counts_books_by_status_for_the_current_user_only()
    {
        var client = await SignedInClientAsync();
        var other = await SignedInClientAsync();
        await CreateAsync(client, "COMPLETED", 100, finishDate: Today);
        await CreateAsync(client, "COMPLETED", 100, finishDate: Today);
        await CreateAsync(client, "READING", 100, currentPage: 10);
        await CreateAsync(client, "TO_READ", 100);
        await CreateAsync(client, "TO_READ", 100);
        await CreateAsync(client, "TO_READ", 100);
        await CreateAsync(other, "READING", 100);

        var overview = await JsonAsync(await client.GetAsync("/api/statistics/overview"));

        Assert.Equal(6, overview.GetProperty("totalBooks").GetInt32());
        Assert.Equal(2, overview.GetProperty("completedBooks").GetInt32());
        Assert.Equal(1, overview.GetProperty("readingBooks").GetInt32());
        Assert.Equal(3, overview.GetProperty("toReadBooks").GetInt32());
    }

    [Fact]
    public async Task Overview_of_a_new_user_is_all_zero()
    {
        var client = await SignedInClientAsync();

        var overview = await JsonAsync(await client.GetAsync("/api/statistics/overview"));

        Assert.Equal(0, overview.GetProperty("totalBooks").GetInt32());
    }

    [Fact]
    public async Task Reading_statistics_use_the_selected_period()
    {
        var client = await SignedInClientAsync();
        // In the last month.
        await CreateAsync(client, "COMPLETED", 300, "Dostoyevski", startDate: Today.AddDays(-40), finishDate: Today.AddDays(-5));
        await CreateAsync(client, "COMPLETED", 201, "Tolstoy", startDate: Today.AddDays(-20), finishDate: Today);
        // In the last 6 months only.
        await CreateAsync(client, "COMPLETED", 400, "Dostoyevski", startDate: Today.AddMonths(-4), finishDate: Today.AddMonths(-3));
        // In the last year only.
        await CreateAsync(client, "COMPLETED", 500, "Orwell", startDate: Today.AddMonths(-11), finishDate: Today.AddMonths(-10));
        // Outside every period.
        await CreateAsync(client, "COMPLETED", 999, "Eski", startDate: Today.AddMonths(-14), finishDate: Today.AddMonths(-13));
        // Reading: current page counts when started in the period.
        await CreateAsync(client, "READING", 500, "Camus", currentPage: 50, startDate: Today.AddDays(-3));
        await CreateAsync(client, "READING", 500, "Camus", currentPage: 70, startDate: Today.AddMonths(-2));
        // To-read books never count.
        await CreateAsync(client, "TO_READ", 800, "Kafka");

        var month = await JsonAsync(await client.GetAsync("/api/statistics/reading?period=1m"));
        Assert.Equal("1m", month.GetProperty("period").GetString());
        Assert.Equal(Today.AddMonths(-1).ToString("yyyy-MM-dd"), month.GetProperty("periodStart").GetString());
        Assert.Equal(Today.ToString("yyyy-MM-dd"), month.GetProperty("periodEnd").GetString());
        Assert.Equal(2, month.GetProperty("completedBooks").GetInt32());
        Assert.Equal(300 + 201 + 50, month.GetProperty("pagesRead").GetInt32());
        Assert.Equal(251, month.GetProperty("averageBookLength").GetInt32()); // 250.5 rounds up

        var halfYear = await JsonAsync(await client.GetAsync("/api/statistics/reading?period=6m"));
        Assert.Equal(3, halfYear.GetProperty("completedBooks").GetInt32());
        Assert.Equal(300 + 201 + 400 + 50 + 70, halfYear.GetProperty("pagesRead").GetInt32());
        Assert.Equal(300, halfYear.GetProperty("averageBookLength").GetInt32());
        var authors = halfYear.GetProperty("booksByAuthor").EnumerateArray()
            .Select(author => (author.GetProperty("author").GetString(), author.GetProperty("count").GetInt32()))
            .ToList();
        Assert.Equal([("Dostoyevski", 2), ("Tolstoy", 1)], authors);

        var year = await JsonAsync(await client.GetAsync("/api/statistics/reading?period=1y"));
        Assert.Equal(4, year.GetProperty("completedBooks").GetInt32());
        Assert.Equal(300 + 201 + 400 + 500 + 50 + 70, year.GetProperty("pagesRead").GetInt32());
    }

    [Fact]
    public async Task Reading_statistics_without_books_are_zero()
    {
        var client = await SignedInClientAsync();

        var stats = await JsonAsync(await client.GetAsync("/api/statistics/reading?period=1y"));

        Assert.Equal(0, stats.GetProperty("completedBooks").GetInt32());
        Assert.Equal(0, stats.GetProperty("pagesRead").GetInt32());
        Assert.Equal(0, stats.GetProperty("averageBookLength").GetInt32());
        Assert.Empty(stats.GetProperty("booksByAuthor").EnumerateArray());
    }

    [Theory]
    [InlineData("")]
    [InlineData("?period=2m")]
    [InlineData("?period=1M")]
    public async Task Unknown_period_is_rejected(string queryString)
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync($"/api/statistics/reading{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await AuthApi.ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Statistics_require_authentication()
    {
        var client = factory.CreateApiClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/statistics/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/statistics/reading?period=1m")).StatusCode);
    }
}
