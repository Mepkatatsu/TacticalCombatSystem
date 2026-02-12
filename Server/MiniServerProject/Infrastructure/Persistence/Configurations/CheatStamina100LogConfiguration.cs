using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniServerProject.Domain.ServerLogs;

namespace MiniServerProject.Infrastructure.Persistence.Configurations
{
    public sealed class CheatStamina100LogConfiguration : IEntityTypeConfiguration<CheatStamina100Log>
    {
        public void Configure(EntityTypeBuilder<CheatStamina100Log> entity)
        {
            entity.ToTable("cheat_stamina100_logs");

            entity.HasKey(x => x.LogId);
            entity.Property(x => x.LogId)
                .HasColumnType("bigint unsigned")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.UserId)
                .HasColumnType("bigint unsigned")
                .IsRequired();

            entity.Property(x => x.RequestId)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.AfterStamina)
                .HasColumnType("smallint unsigned")
                .IsRequired();

            entity.HasIndex(x => new { x.UserId, x.RequestId })
                .IsUnique();
        }
    }
}
