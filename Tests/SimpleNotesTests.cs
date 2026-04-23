using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using SimpleNotesApiTests.DTOs;

namespace SimpleNotesApiTests.Tests;

[TestFixture]
[NonParallelizable]
public class SimpleNotesTests
{
    private const string BaseApiUrl = "http://144.91.123.158:5005/api/";
    private RestClient client = null!;
    private static string createdNoteId = string.Empty;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var testUser = GenerateRandomUser();
        RegisterUser(testUser);
        var accessToken = LoginAndGetAccessToken(testUser.Email, testUser.Password);

        var options = new RestClientOptions(BaseApiUrl)
        {
            Authenticator = new JwtAuthenticator(accessToken)
        };

        client = new RestClient(options);
    }

    [Test]
    [Order(1)]
    public void CreateNoteWithoutRequiredFields_ShouldReturnBadRequest()
    {
        var request = new RestRequest("Note/Create", Method.Post);
        request.AddJsonBody(new { });

        var response = client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    [Order(2)]
    public void CreateNoteWithRequiredFields_ShouldReturnOk()
    {
        var request = new RestRequest("Note/Create", Method.Post);
        request.AddJsonBody(new
        {
            title = "Valid Note Title",
            description = "This is a valid note description created by the automated API test.",
            status = "New"
        });

        var response = client.Execute<ApiResponseDto>(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data!.Msg, Is.EqualTo("Note created successfully!"));
    }

    [Test]
    [Order(3)]
    public void GetAllNotes_ShouldReturnNotesAndStoreLastNoteId()
    {
        var request = new RestRequest("Note/AllNotes", Method.Get);

        var response = client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var notes = JsonSerializer.Deserialize<List<NoteDto>>(
            JsonDocument.Parse(response.Content!)
                .RootElement
                .GetProperty("allNotes")
                .GetRawText());

        Assert.That(notes, Is.Not.Null);
        Assert.That(notes!, Is.Not.Empty);

        createdNoteId = notes.Last().Id;

        Assert.That(createdNoteId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Order(4)]
    public void EditCreatedNote_ShouldReturnOk()
    {
        Assert.That(createdNoteId, Is.Not.Null.And.Not.Empty);

        var request = new RestRequest($"Note/Edit/{createdNoteId}", Method.Put);
        request.AddJsonBody(new
        {
            title = "Edited Note Title",
            description = "This note was edited successfully by the automated API test.",
            status = "Done"
        });

        var response = client.Execute<ApiResponseDto>(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data!.Msg, Is.EqualTo("Note edited successfully!"));
    }

    [Test]
    [Order(5)]
    public void DeleteCreatedNote_ShouldReturnOk()
    {
        Assert.That(createdNoteId, Is.Not.Null.And.Not.Empty);

        var request = new RestRequest($"Note/Delete/{createdNoteId}", Method.Delete);

        var response = client.Execute<ApiResponseDto>(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data!.Msg, Is.EqualTo("Note deleted successfully!"));
    }

    private static TestUserData GenerateRandomUser()
    {
        var uniquePart = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

        return new TestUserData
        {
            UserName = $"user{uniquePart}",
            FirstName = "Test",
            LastName = "User",
            Email = $"user{uniquePart}@example.com",
            Password = $"Pass{uniquePart}!"
        };
    }

    private static void RegisterUser(TestUserData user)
    {
        var client = new RestClient(new RestClientOptions(BaseApiUrl));

        var request = new RestRequest("User/Register", Method.Post);
        request.AddJsonBody(new
        {
            userName = user.UserName,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
            password = user.Password,
            rePassword = user.Password
        });

        var response = client.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
        {
            Assert.Fail($"User registration failed. Status: {response.StatusCode}. Body: {response.Content}");
        }
    }

    private static string LoginAndGetAccessToken(string email, string password)
    {
        var client = new RestClient(new RestClientOptions(BaseApiUrl));

        var request = new RestRequest("User/Authorization", Method.Post);
        request.AddJsonBody(new
        {
            email,
            password
        });

        var response = client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var jsonDocument = JsonDocument.Parse(response.Content!);
        var accessToken = jsonDocument.RootElement.GetProperty("accessToken").GetString();

        Assert.That(accessToken, Is.Not.Null.And.Not.Empty);

        return accessToken!;
    }

    private class TestUserData
    {
        public string UserName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
