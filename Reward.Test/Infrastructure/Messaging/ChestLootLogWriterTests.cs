using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Reward.Application.Messaging;
using Reward.Domain.Services;
using Reward.Infrastructure.Messaging;

namespace Reward.Test.Infrastructure.Messaging;

public sealed class ChestLootLogWriterTests
{
    [Fact]
    public async Task TryWriteCapacityRefusedAsync_WhenScopeCreationFails_DoesNotMaskRefusal()
    {
        // Arrange a logging infrastructure outage before a fresh database scope can be opened.
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory
            .Setup(factory => factory.CreateScope())
            .Throws(new InvalidOperationException("Outbox database unavailable."));
        var writer = new ChestLootLogWriter(
            scopeFactory.Object,
            Mock.Of<IClock>(),
            Mock.Of<ILogger<ChestLootLogWriter>>()
        );

        // Act
        Func<Task> action = () =>
            writer.TryWriteCapacityRefusedAsync(
                new RefusedChestLootClaimLog(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                TestContext.Current.CancellationToken
            );

        // Assert the caller remains free to return the original capacity ProblemDetails.
        await action.Should().NotThrowAsync();
    }
}
