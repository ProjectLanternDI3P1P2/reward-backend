using Reward.Application.Abstractions;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using Reward.Infrastructure.PipelineBehavior;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Reward.Test.PipelineBehavior;

public sealed class CommandTransactionBehaviorTests
{
    [Fact]
    public async Task Handle_CommandSucceeds_CommitsChanges()
    {
        string databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateInMemoryDbContext(databaseName);
        var behavior = new CommandTransactionBehavior<CreateCategoryCommand, Unit>(dbContext);
        var category = new Category { Id = Guid.NewGuid(), Label = "Committed" };

        var result = await behavior.Handle(
            new CreateCategoryCommand(category),
            _ =>
            {
                dbContext.Categories.Add(category);
                return Task.FromResult(Unit.Value);
            },
            TestContext.Current.CancellationToken);

        result.Should().Be(Unit.Value);
        await using var verificationContext = CreateInMemoryDbContext(databaseName);
        (await verificationContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_CommandFails_DoesNotCommitChanges()
    {
        string databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateInMemoryDbContext(databaseName);
        var behavior = new CommandTransactionBehavior<CreateCategoryCommand, Unit>(dbContext);
        var category = new Category { Id = Guid.NewGuid(), Label = "Not committed" };

        Func<Task> action = async () => await behavior.Handle(
            new CreateCategoryCommand(category),
            _ =>
            {
                dbContext.Categories.Add(category);
                return Task.FromException<Unit>(new InvalidOperationException("Handler failed."));
            },
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>();
        await using var verificationContext = CreateInMemoryDbContext(databaseName);
        (await verificationContext.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken))
            .Should().BeNull();
    }

    private static RewardDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<RewardDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new RewardDbContext(options);
    }

    private sealed record CreateCategoryCommand(Category Category) : ICommand;
}
