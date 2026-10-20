using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;

namespace Reward.Test.Integration.Placeholders.Controllers;

public sealed class GetPlaceholderByIdIntegrationTests(PlaceholdersControllerFixture fixture)
    : PlaceholdersControllerTestBase(fixture)
{
    [Fact]
    public async Task GetById_WhenPlaceholderExists_ReturnsIt()
    {
        Guid placeholderId = Guid.NewGuid();
        await using (AsyncServiceScope scope = Fixture.Services.CreateAsyncScope())
        {
            RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
            dbContext.Placeholders.Add(
                new Placeholder { Id = placeholderId, Name = "Starter chest" }
            );
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        HttpResponseMessage response = await Fixture.HttpClient.GetAsync(
            $"/api/v1/placeholders/{placeholderId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PlaceholderResponse? body = await response.Content.ReadFromJsonAsync<PlaceholderResponse>(
            TestContext.Current.CancellationToken
        );
        body.Should().BeEquivalentTo(new PlaceholderResponse(placeholderId, "Starter chest"));
    }

    [Fact]
    public async Task GetById_WhenPlaceholderDoesNotExist_ReturnsNotFound()
    {
        HttpResponseMessage response = await Fixture.HttpClient.GetAsync(
            $"/api/v1/placeholders/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record PlaceholderResponse(Guid Id, string Name);
}
