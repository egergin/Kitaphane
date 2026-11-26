using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

/// <summary>"Okuduğum Kitaplar": completed books list and taking a book off the list.</summary>
[Collection(ApiCollection.Name)]
public sealed class CompletedBooksTests(KitaphaneApiFactory factory)
{
    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        var session = await new AuthApi(client, factory.Emails).CreateAccountAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private static async Task<string> CreateAsync(HttpClient client, string title, string status, string? finishDate = null, int? rating = null)
    {
        var response = await client.PostAsJsonAsync("/api/books", new
        {
            title,
            author = "Yazar",
            pageCount = 200,
            currentPage = 0,
            status,
            isbn = (string?)null,
            rating,
            startDate = finishDate is null ? null : "2025-01-01",
            finishDate,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Completed_list_contains_only_completed_books_most_recently_finished_first()
    {
        var client = await SignedInClientAsync();
        await CreateAsync(client, "Eski", "COMPLETED", "2025-03-01", rating: 7);
        await CreateAsync(client, "Yeni", "COMPLETED", "2025-11-20", rating: 9);
        await CreateAsync(client, "Orta", "COMPLETED", "2025-06-15");
        await CreateAsync(client, "Okunuyor", "READING");
        await CreateAsync(client, "Okunacak", "TO_READ");

        var page = await JsonAsync(await client.GetAsync("/api/books/completed"));
        var items = page.GetProperty("items");

        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(["Yeni", "Orta", "Eski"], items.EnumerateArray().Select(item => item.GetProperty("title").GetString()));

        var first = items[0];
        Assert.Equal("2025-11-20", first.GetProperty("finishDate").GetString());
        Assert.Equal(9, first.GetProperty("rating").GetInt32());
        Assert.Equal(200, first.GetProperty("pageCount").GetInt32());
        Assert.True(first.TryGetProperty("isbn", out _));
        Assert.False(first.TryGetProperty("notes", out _));
    }

    [Fact]
    public async Task Completed_list_is_paginated_and_scoped_to_the_user()
    {
        var client = await SignedInClientAsync();
        var other = await SignedInClientAsync();
        for (var i = 1; i <= 19; i++)
        {
            await CreateAsync(client, $"Kitap {i}", "COMPLETED", $"2025-01-{i:00}");
        }

        await CreateAsync(other, "Başkasının kitabı", "COMPLETED", "2025-12-01");

        var first = await JsonAsync(await client.GetAsync("/api/books/completed?page=1&pageSize=18"));
        var second = await JsonAsync(await client.GetAsync("/api/books/completed?page=2&pageSize=18"));

        Assert.Equal(19, first.GetProperty("totalCount").GetInt32());
        Assert.True(first.GetProperty("hasNext").GetBoolean());
        Assert.Equal(18, first.GetProperty("items").GetArrayLength());
        Assert.Single(second.GetProperty("items").EnumerateArray());
        Assert.DoesNotContain(
            first.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("title").GetString() == "Başkasının kitabı");
    }

    [Fact]
    public async Task Completed_list_can_be_searched_by_title_or_author()
    {
        var client = await SignedInClientAsync();
        await CreateAsync(client, "İnce Memed", "COMPLETED", "2025-05-01");
        await CreateAsync(client, "Kürk Mantolu Madonna", "COMPLETED", "2025-06-01");
        await CreateAsync(client, "İnce Ruhlar", "READING");

        var byTitle = await JsonAsync(await client.GetAsync("/api/books/completed?search=ince"));
        Assert.Equal(1, byTitle.GetProperty("totalCount").GetInt32());
        Assert.Equal("İnce Memed", byTitle.GetProperty("items")[0].GetProperty("title").GetString());

        var byAuthor = await JsonAsync(await client.GetAsync("/api/books/completed?search=yazar"));
        Assert.Equal(2, byAuthor.GetProperty("totalCount").GetInt32());

        var none = await JsonAsync(await client.GetAsync("/api/books/completed?search=dostoyevski"));
        Assert.Equal(0, none.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Taking_a_book_off_the_list_sets_it_back_to_reading()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client, "Yanlışlıkla bitti", "COMPLETED", "2025-11-01");

        var response = await client.PutAsJsonAsync($"/api/books/{id}/status", new { status = "READING" });
        var book = await JsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("READING", book.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, book.GetProperty("finishDate").ValueKind);
        Assert.Equal("2025-01-01", book.GetProperty("startDate").GetString()); // kept

        var list = await JsonAsync(await client.GetAsync("/api/books/completed"));
        Assert.Equal(0, list.GetProperty("totalCount").GetInt32());

        // The book itself still exists.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/books/{id}")).StatusCode);
    }

    [Fact]
    public async Task Status_change_of_another_users_book_returns_not_found()
    {
        var owner = await SignedInClientAsync();
        var other = await SignedInClientAsync();
        var id = await CreateAsync(owner, "Benim", "COMPLETED", "2025-11-01");

        var response = await other.PutAsJsonAsync($"/api/books/{id}/status", new { status = "READING" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var list = await JsonAsync(await owner.GetAsync("/api/books/completed"));
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Unknown_status_is_rejected()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client, "Kitap", "COMPLETED", "2025-11-01");

        var response = await client.PutAsJsonAsync($"/api/books/{id}/status", new { status = "DONE" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
