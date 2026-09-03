using System.Net;
using System.Net.Http.Json;
using DevTasks.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace DevTasks.IntegrationTests
{
    public class TasksControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public TasksControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> RegisterAndLoginAsync(string email, string password = "Password123!")
        {
            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Test User",
                Email = email,
                Password = password
            });

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = email,
                Password = password
            });

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResult!.Token;
        }

        private record LoginResponse(string Token, int ExpiresInMinutes);

        [Fact]
        public async Task GetAll_WithoutAuthToken_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/tasks");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTask_ThenGetAll_ReturnsOnlyOwnTask()
        {
            // Arrange
            var token = await RegisterAndLoginAsync($"user_{Guid.NewGuid()}@test.com");
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createDto = new CreateTaskDto { Title = "Integration Test Task", Description = "Via HTTP" };

            // Act
            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto);
            var getAllResponse = await _client.GetAsync("/api/tasks");
            var tasks = await getAllResponse.Content.ReadFromJsonAsync<List<TaskItemDto>>();

            // Assert
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            tasks.Should().ContainSingle(t => t.Title == "Integration Test Task");
        }

        [Fact]
        public async Task GetById_WhenNotOwner_ReturnsForbidden()
        {
            // Arrange — User A creates a task
            var tokenA = await RegisterAndLoginAsync($"userA_{Guid.NewGuid()}@test.com");
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

            var createResponse = await _client.PostAsJsonAsync("/api/tasks",
                new CreateTaskDto { Title = "User A's Task" });
            var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

            // Act — User B tries to access it
            var tokenB = await RegisterAndLoginAsync($"userB_{Guid.NewGuid()}@test.com");
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenB);

            var response = await _client.GetAsync($"/api/tasks/{createdTask!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}