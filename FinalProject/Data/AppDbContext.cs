using FinalProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<LocalBeer> LocalBeers { get; set; } = default!;
        public DbSet<QuickRating> QuickRatings { get; set; } = default!;
        public DbSet<BeerComment> BeerComments { get; set; } = default!;
        public DbSet<UserStats> UserStats { get; set; } = default!;
        public DbSet<BeerFavorite> BeerFavorites { get; set; } = default!;

        // 🆕 DbSet ใหม่
        public DbSet<LocalBeerFlavor> LocalBeerFlavors { get; set; } = default!;
        public DbSet<LocalBeerFoodPairing> LocalBeerFoodPairings { get; set; } = default!;
        public DbSet<LocalBeerMoodPairing> LocalBeerMoodPairings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<QuickRating>(e =>
            {
                e.ToTable("QuickRatings");
                e.HasKey(x => x.Id);
                e.Property(x => x.Score).IsRequired();
                e.Property(x => x.Fingerprint).HasMaxLength(128).IsRequired();
                e.Property(x => x.IpHash).HasMaxLength(128);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(x => new { x.LocalBeerId, x.Fingerprint }).IsUnique();
            });

            modelBuilder.Entity<LocalBeer>(e =>
            {
                e.Property(x => x.Rating).HasColumnType("float");
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                e.Property(x => x.RatingCount).HasDefaultValue(0);

                // ✅ ความสัมพันธ์
                e.HasMany(b => b.Flavors)
                 .WithOne(f => f.LocalBeer!)
                 .HasForeignKey(f => f.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(b => b.FoodPairings)
                 .WithOne(fp => fp.LocalBeer!)
                 .HasForeignKey(fp => fp.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(b => b.MoodPairings)
                 .WithOne(mp => mp.LocalBeer!)
                 .HasForeignKey(mp => mp.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BeerComment>(e =>
            {
                e.ToTable("BeerComments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Body).IsRequired().HasMaxLength(1000);
                e.Property(x => x.DisplayName).HasMaxLength(100);
                e.Property(x => x.UserName).HasMaxLength(100);
                e.Property(x => x.IpHash).HasMaxLength(128);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(x => new { x.LocalBeerId, x.CreatedAt });
            });

            modelBuilder.Entity<UserStats>(e =>
            {
                e.HasKey(s => s.UserId);
                e.HasOne(s => s.User)
                 .WithOne()
                 .HasForeignKey<UserStats>(s => s.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BeerFavorite>(e =>
            {
                e.ToTable("BeerFavorites");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.UserId, x.LocalBeerId }).IsUnique();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.LocalBeer)
                 .WithMany()
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
