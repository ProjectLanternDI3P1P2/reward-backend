using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Reward.Domain.Entities;

namespace Reward.Infrastructure.Persistence.Configurations;

public sealed class OwnershipTransferConfiguration : IEntityTypeConfiguration<OwnershipTransfer>
{
    public void Configure(EntityTypeBuilder<OwnershipTransfer> builder)
    {
        builder.ToTable("ownership_transfer");
    }
}
