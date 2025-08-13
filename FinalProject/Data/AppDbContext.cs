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

                // ✅ กำหนด precision ให้ Price เพื่อไม่ให้เกิด warning
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");

                e.Property(x => x.RatingCount).HasDefaultValue(0);
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
                 .WithOne()                     // หรือ .WithOne(u => u.Stats) ถ้าคุณเปิด nav property ข้างบน
                 .HasForeignKey<UserStats>(s => s.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BeerFavorite>(e =>
            {
                e.ToTable("BeerFavorites");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new { x.UserId, x.LocalBeerId }).IsUnique(); // 1 คน/1 เบียร์ ได้กดได้ครั้งเดียว
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
