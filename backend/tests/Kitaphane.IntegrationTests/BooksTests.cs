using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kitaphane.IntegrationTests.Infrastructure;

namespace Kitaphane.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class BooksTests(KitaphaneApiFactory factory)
{
    /// <summary>Creates a new account and returns a client that sends its access token.</summary>
    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        var session = await new AuthApi(client, factory.Emails).CreateAccountAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    private static object NewBook(string title = "Suç ve Ceza", string? isbn = null, string status = "TO_READ", int currentPage = 0) => new
    {
        title,
        author = "Fyodor Dostoyevski",
        pageCount = 687,
        currentPage,
        status,
        isbn,
    };

    private static async Task<JsonElement> CreateAsync(HttpClient client, object book)
    {
        var response = await client.PostAsJsonAsync("/api/books", book);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Book_can_be_created_read_updated_and_deleted()
    {
        var client = await SignedInClientAsync();

        var created = await CreateAsync(client, NewBook(isbn: "978-0-321-82248-2"));
        var id = created.GetProperty("id").GetString();
        Assert.Equal("TO_READ", created.GetProperty("status").GetString());
        Assert.Equal("9780321822482", created.GetProperty("isbn").GetString());
        Assert.Equal(0, created.GetProperty("currentPage").GetInt32());

        var get = await client.GetAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var update = await client.PutAsJsonAsync($"/api/books/{id}", new
        {
            title = "Suç ve Ceza (Yeni Baskı)",
            author = "Fyodor Dostoyevski",
            pageCount = 700,
            currentPage = 250,
            status = "READING",
            rating = 9,
            notes = "Raskolnikov",
            startDate = "2026-11-01",
        });
        var updated = await JsonAsync(update);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Suç ve Ceza (Yeni Baskı)", updated.GetProperty("title").GetString());
        Assert.Equal(250, updated.GetProperty("currentPage").GetInt32());
        Assert.Equal(9, updated.GetProperty("rating").GetInt32());
        Assert.Equal("2026-11-01", updated.GetProperty("startDate").GetString());

        var delete = await client.DeleteAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await client.GetAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
        Assert.Equal("BOOK_NOT_FOUND", (await JsonAsync(afterDelete)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Users_cannot_see_or_change_each_others_books()
    {
        var owner = await SignedInClientAsync();
        var other = await SignedInClientAsync();
        var id = (await CreateAsync(owner, NewBook())).GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/books/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/books/{id}", NewBook())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/books/{id}")).StatusCode);

        var otherList = await JsonAsync(await other.GetAsync("/api/books"));
        Assert.Equal(0, otherList.GetProperty("totalCount").GetInt32());

        // The owner's book is untouched.
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/books/{id}")).StatusCode);
    }

    [Fact]
    public async Task Books_require_authentication()
    {
        var anonymous = factory.CreateApiClient();

        var response = await anonymous.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_is_paginated_and_most_recently_updated_first()
    {
        var client = await SignedInClientAsync();
        var ids = new List<string>();
        for (var i = 1; i <= 20; i++)
        {
            ids.Add((await CreateAsync(client, NewBook(title: $"Kitap {i}"))).GetProperty("id").GetString()!);
        }

        // Updating the first book moves it to the top.
        await client.PutAsJsonAsync($"/api/books/{ids[0]}", new
        {
            title = "Kitap 1", author = "Yazar", pageCount = 100, currentPage = 10, status = "READING",
        });

        var firstPage = await JsonAsync(await client.GetAsync("/api/books?page=1&pageSize=18"));
        var items = firstPage.GetProperty("items");
        Assert.Equal(18, items.GetArrayLength());
        Assert.Equal(20, firstPage.GetProperty("totalCount").GetInt32());
        Assert.True(firstPage.GetProperty("hasNext").GetBoolean());
        Assert.Equal(ids[0], items[0].GetProperty("id").GetString());
        Assert.Equal("Kitap 20", items[1].GetProperty("title").GetString());

        // List items carry only what the home page card needs.
        var card = items[0];
        Assert.Equal(10, card.GetProperty("currentPage").GetInt32());
        Assert.Equal(100, card.GetProperty("pageCount").GetInt32());
        Assert.False(card.TryGetProperty("notes", out _));

        var secondPage = await JsonAsync(await client.GetAsync("/api/books?page=2&pageSize=18"));
        Assert.Equal(2, secondPage.GetProperty("items").GetArrayLength());
        Assert.False(secondPage.GetProperty("hasNext").GetBoolean());
    }

    [Theory]
    [InlineData("çalı", "Çalıkuşu")]           // Turkish letters, case-insensitive
    [InlineData("ÇALIKUŞU", "Çalıkuşu")]       // upper-case dotless i
    [InlineData("INCE", "İnce Memed")]         // dotted capital İ
    [InlineData("İNCE", "İnce Memed")]         // dotted capital İ in the search text
    [InlineData("GÜNTEKİN", "Çalıkuşu")]       // author, upper case with İ
    [InlineData("kemal", "İnce Memed")]        // author match
    [InlineData("  memed ", "İnce Memed")]     // trimmed
    public async Task Search_matches_title_or_author(string search, string expectedTitle)
    {
        var client = await SignedInClientAsync();
        await CreateAsync(client, new { title = "Çalıkuşu", author = "Reşat Nuri Güntekin", pageCount = 544 });
        await CreateAsync(client, new { title = "İnce Memed", author = "Yaşar Kemal", pageCount = 436 });
        await CreateAsync(client, new { title = "Suç ve Ceza", author = "Fyodor Dostoyevski", pageCount = 687 });

        var page = await JsonAsync(await client.GetAsync($"/api/books?search={Uri.EscapeDataString(search)}"));
        var titles = page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("title").GetString()).ToList();

        Assert.Equal([expectedTitle], titles);
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_with_no_match_returns_empty_page()
    {
        var client = await SignedInClientAsync();
        await CreateAsync(client, NewBook());

        var page = await JsonAsync(await client.GetAsync("/api/books?search=zzzz"));

        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Invalid_page_size_is_rejected()
    {
        var client = await SignedInClientAsync();

        var response = await client.GetAsync("/api/books?pageSize=20");
        var body = await JsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.GetProperty("errors").TryGetProperty("pageSize", out _));
    }

    [Fact]
    public async Task Duplicate_isbn_is_rejected_for_the_same_user_only()
    {
        var client = await SignedInClientAsync();
        var otherUser = await SignedInClientAsync();
        await CreateAsync(client, NewBook(isbn: "9780321822482"));

        var duplicate = await client.PostAsJsonAsync("/api/books", NewBook(title: "Kopya", isbn: "978-0321822482"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("BOOK_ISBN_EXISTS", (await JsonAsync(duplicate)).GetProperty("code").GetString());

        // Another user may add the same book (no shared catalog).
        await CreateAsync(otherUser, NewBook(isbn: "9780321822482"));

        // Books without ISBN are never treated as duplicates.
        await CreateAsync(client, NewBook(title: "ISBN'siz 1"));
        await CreateAsync(client, NewBook(title: "ISBN'siz 2"));
    }

    [Fact]
    public async Task Completing_a_book_applies_status_rules()
    {
        var client = await SignedInClientAsync();
        var id = (await CreateAsync(client, NewBook(status: "READING", currentPage: 100))).GetProperty("id").GetString();

        var completed = await JsonAsync(await client.PutAsJsonAsync($"/api/books/{id}", new
        {
            title = "Suç ve Ceza", author = "Fyodor Dostoyevski", pageCount = 687, currentPage = 100,
            status = "COMPLETED", startDate = "2026-10-01",
        }));

        Assert.Equal(687, completed.GetProperty("currentPage").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, completed.GetProperty("finishDate").ValueKind);

        var reopened = await JsonAsync(await client.PutAsJsonAsync($"/api/books/{id}", new
        {
            title = "Suç ve Ceza", author = "Fyodor Dostoyevski", pageCount = 687, currentPage = 687,
            status = "READING", startDate = "2026-10-01", finishDate = "2026-11-20",
        }));

        Assert.Equal(JsonValueKind.Null, reopened.GetProperty("finishDate").ValueKind);
        Assert.Equal("2026-10-01", reopened.GetProperty("startDate").GetString());
    }

    [Fact]
    public async Task New_reading_book_gets_a_start_date()
    {
        var client = await SignedInClientAsync();

        var created = await CreateAsync(client, NewBook(status: "READING"));

        Assert.NotEqual(JsonValueKind.Null, created.GetProperty("startDate").ValueKind);
    }

    [Fact]
    public async Task Invalid_book_returns_field_errors()
    {
        var client = await SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/books", new
        {
            title = "",
            author = "Yazar",
            pageCount = 100,
            currentPage = 150,
            rating = 11,
        });
        var errors = (await JsonAsync(response)).GetProperty("errors");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(errors.TryGetProperty("title", out _));
        Assert.True(errors.TryGetProperty("currentPage", out _));
        Assert.True(errors.TryGetProperty("rating", out _));
    }

    [Fact]
    public async Task Unknown_status_value_is_rejected()
    {
        var client = await SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/books", NewBook(status: "FINISHED"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await JsonAsync(response)).GetProperty("code").GetString());
    }
}
