using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using TestStorySpoiler.Models;

namespace TestStorySpoiler
{


    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private static string CreatedStoryID;
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net/";
        private string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI3Mjg2NWRjZi1lNGRlLTQxYjktYWRiNy0zMmY5MzgwMjQ4MDMiLCJpYXQiOiIwOC8xNi8yMDI1IDA2OjMxOjM2IiwiVXNlcklkIjoiOWEyYTAxOGYtY2Y1MC00ODhjLThlMTAtMDhkZGRiMWExM2YzIiwiRW1haWwiOiJwaWZhQHByZXBhcmF0aW9uLmNvbSIsIlVzZXJOYW1lIjoicGlmYSIsImV4cCI6MTc1NTM0NzQ5NiwiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.7639yPrcNmUn9IqaWrOktbOeoK12ysxiAY4ihEWbVTE";
        private const string LoginEmail = "pifa@preparation.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

        //All test here

        [Order(1)]
        [Test]
        public void CreateStory_WithRequiredFields_ShouldReturnSuccess()
        {
            var storyRequest = new StoryDTO
            {
                Title = "Test Story",
                Description = "This is a test story description.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));

            Assert.That(createResponse.Id, Is.Not.Null.Or.Empty, "Story ID was not returned in the response.");


            CreatedStoryID = createResponse.Id;



        }



        [Order(2)]
        [Test]

        public void EditExistingStory_ShouldReturnSuccess()
        {
            var editRequest = new StoryDTO
            {
                Title = "Edited Story",
                Description = "This is an updated test story description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{CreatedStoryID}", Method.Put);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));

        }

        [Order(3)]
        [Test]
        public void GetAllstorys_ShouldReturnListOfstorys()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = this.client.Execute(request);

            var responsItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);


        }

        [Order(4)]
        [Test]
        public void DeleteStory_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Story/Delete/{CreatedStoryID}", Method.Delete);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Order(5)]
        [Test]

        public void CreateStory_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var StoryRequest = new StoryDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(StoryRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(6)]
        [Test]

        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "123";
            var editRequest = new StoryDTO
            {
                Title = "Edited Non-Existing Story",
                Description = "This is an updated test Story description for a non-existing Story.",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers.."));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            string nonExistingStoryId = "123";
            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }

    }




}