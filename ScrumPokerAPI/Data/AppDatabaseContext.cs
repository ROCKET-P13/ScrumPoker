using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Entities;

namespace ScrumPokerAPI.Data;

public class AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Participant> Participants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
			entity.ToTable("Rooms");
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.Code).HasColumnName("code");
			entity.Property(e => e.IsRevealed).HasColumnName("is_revealed");
			entity.Property(e => e.CreatedAt).HasColumnName("created_at");
			entity.Property(e => e.EmptySince).HasColumnName("empty_since");

            entity.HasKey(room => room.Id);
            entity.HasIndex(room => room.Code).IsUnique();
            entity.Property(room => room.Code).HasMaxLength(32).IsRequired();

            entity.HasMany(room => room.Participants)
				.WithOne()
                .HasForeignKey(participant => participant.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(entity =>
        {
			entity.ToTable("Participants");
			entity.Property(e => e.Id).HasColumnName("id");
			entity.Property(e => e.RoomId).HasColumnName("room_id");
			entity.Property(e => e.ConnectionId).HasColumnName("connection_id");
			entity.Property(e => e.DisplayName).HasColumnName("display_name");
			entity.Property(e => e.IsRoomAdmin).HasColumnName("is_room_admin");
			entity.Property(e => e.Vote).HasColumnName("vote");

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConnectionId).IsUnique();
            entity.Property(e => e.ConnectionId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Vote).HasMaxLength(32);
        });
    }
}
