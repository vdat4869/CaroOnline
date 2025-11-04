using Caro.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caro.Infrastructure;

public class CaroDbContext : DbContext
{
    public CaroDbContext(DbContextOptions<CaroDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Move> Moves => Set<Move>();
    public DbSet<GameHistory> GameHistory => Set<GameHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Game>(b =>
        {
            b.ToTable("Games");
            b.HasKey(x => x.Id);
            b.HasMany(x => x.Moves).WithOne().HasForeignKey(m => m.GameId);
            b.HasMany(x => x.History).WithOne().HasForeignKey(h => h.GameId);
        });

        modelBuilder.Entity<Move>(b =>
        {
            b.ToTable("Moves");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.GameId, x.MoveNumber }).IsUnique();
        });

        modelBuilder.Entity<GameHistory>(b =>
        {
            b.ToTable("GameHistory");
            b.HasKey(x => x.Id);
        });
    }
}


