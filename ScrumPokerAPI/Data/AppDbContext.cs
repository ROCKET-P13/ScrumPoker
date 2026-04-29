using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(roomEntity =>
        {
            roomEntity.HasKey(room => room.Id);
            roomEntity.HasIndex(room => room.Code).IsUnique();
            roomEntity.Property(room => room.Code).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<Participant>(participantEntity =>
        {
            participantEntity.HasKey(participant => participant.Id);
            participantEntity.HasIndex(participant => participant.ConnectionId).IsUnique();
            participantEntity.Property(participant => participant.ConnectionId).HasMaxLength(128).IsRequired();
            participantEntity.Property(participant => participant.DisplayName).HasMaxLength(256).IsRequired();
            participantEntity.Property(participant => participant.VoteValue).HasMaxLength(32);
            participantEntity.HasOne(participant => participant.Room)
                .WithMany(room => room.Participants)
                .HasForeignKey(participant => participant.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
