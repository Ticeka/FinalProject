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
        public DbSet<ActivityLog> ActivityLogs { get; set; } = default!;

        // แนวคู่รส/อาหาร/อารมณ์
        public DbSet<LocalBeerFlavor> LocalBeerFlavors { get; set; } = default!;
        public DbSet<LocalBeerFoodPairing> LocalBeerFoodPairings { get; set; } = default!;
        public DbSet<LocalBeerMoodPairing> LocalBeerMoodPairings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== LocalBeer =====
            modelBuilder.Entity<LocalBeer>(e =>
            {
                e.Property(x => x.Rating).HasColumnType("float");
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                e.Property(x => x.RatingCount).HasDefaultValue(0);

                e.HasIndex(x => x.Province);
                e.HasIndex(x => x.Name);

                // Relations to pairings
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

            // ===== QuickRating =====
            modelBuilder.Entity<QuickRating>(e =>
            {
                e.ToTable("QuickRatings");
                e.HasKey(x => x.Id);

                e.Property(x => x.Score).IsRequired();
                e.HasCheckConstraint("CK_QuickRatings_Score", "[Score] >= 0 AND [Score] <= 5");

                e.Property(x => x.Fingerprint).HasMaxLength(128).IsRequired();
                e.Property(x => x.IpHash).HasMaxLength(128);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // ป้องกันกดซ้ำจาก fingerprint เดียวกันในเบียร์เดียวกัน
                e.HasIndex(x => new { x.LocalBeerId, x.Fingerprint }).IsUnique();

                // ช่วย GroupBy ต่อ user
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.LocalBeerId);

                // User ↔ QuickRating
                e.HasOne<ApplicationUser>()
                 .WithMany(u => u.Ratings)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);

                // 1 เบียร์ / 1 ผู้ใช้ ได้เรตครั้งเดียว (เฉพาะเมื่อล็อกอิน)
                e.HasIndex(x => new { x.LocalBeerId, x.UserId })
                 .IsUnique()
                 .HasFilter("[UserId] IS NOT NULL");
            });

            // ===== BeerComment =====
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
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.LocalBeerId);

                e.HasOne<ApplicationUser>()
                 .WithMany(u => u.Comments)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasOne<LocalBeer>()
                 .WithMany()
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== UserStats (one-to-one) =====
            modelBuilder.Entity<UserStats>(e =>
            {
                e.HasKey(s => s.UserId);

                e.HasOne(s => s.User)
                 .WithOne(u => u.Stats)
                 .HasForeignKey<UserStats>(s => s.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(s => s.Reviews).HasDefaultValue(0);
                e.Property(s => s.Comments).HasDefaultValue(0);
                e.Property(s => s.Favorites).HasDefaultValue(0);
                e.Property(s => s.Badges).HasDefaultValue(0);
            });

            // ===== BeerFavorite =====
            modelBuilder.Entity<BeerFavorite>(e =>
            {
                e.ToTable("BeerFavorites");
                e.HasKey(x => x.Id);

                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasIndex(x => new { x.UserId, x.LocalBeerId }).IsUnique();

                e.HasOne(x => x.User)
                 .WithMany(u => u.Favorites)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.LocalBeer)
                 .WithMany()
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Flavor / FoodPairing / MoodPairing =====
            modelBuilder.Entity<LocalBeerFlavor>(e =>
            {
                e.ToTable("LocalBeerFlavors");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.LocalBeerId);

                // ใช้ Flavor (ไม่ใช่ Name)
                e.Property(x => x.Flavor)
                 .HasMaxLength(100)
                 .IsRequired();

                // 0..1 ตามโมเดลของคุณ (จะใช้ 0..100 ก็ปรับ constraint ได้)
                e.HasCheckConstraint("CK_LocalBeerFlavor_Intensity", "[Intensity] >= 0 AND [Intensity] <= 1");

                e.HasOne(x => x.LocalBeer)
                 .WithMany(b => b.Flavors)
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LocalBeerFoodPairing>(e =>
            {
                e.ToTable("LocalBeerFoodPairings");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.LocalBeerId);

                // ใช้ FoodName (ไม่ใช่ Name)
                e.Property(x => x.FoodName)
                 .HasMaxLength(100)
                 .IsRequired();

                e.Property(x => x.Reason).HasMaxLength(500);

                e.HasOne(x => x.LocalBeer)
                 .WithMany(b => b.FoodPairings)
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LocalBeerMoodPairing>(e =>
            {
                e.ToTable("LocalBeerMoodPairings");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.LocalBeerId);

                // ใช้ Mood (ไม่ใช่ Name)
                e.Property(x => x.Mood)
                 .HasMaxLength(100)
                 .IsRequired();

                e.Property(x => x.Reason).HasMaxLength(500);

                e.HasOne(x => x.LocalBeer)
                 .WithMany(b => b.MoodPairings)
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ActivityLog =====
            modelBuilder.Entity<ActivityLog>(e =>
            {
                e.ToTable("ActivityLogs");
                e.HasKey(x => x.Id);

                // โครงสร้างคอลัมน์ (กำหนดความยาว/ค่าเริ่มต้นให้ชัดเจน ป้องกัน mismatch)
                e.Property(x => x.Action).IsRequired().HasMaxLength(64);
                e.Property(x => x.SubjectType).HasMaxLength(64);
                e.Property(x => x.SubjectId).HasMaxLength(128);
                e.Property(x => x.Message).IsRequired().HasMaxLength(300);
                e.Property(x => x.MetaJson);                  // nvarchar(max)
                e.Property(x => x.IpHash).HasMaxLength(128);
                e.Property(x => x.UserAgent).HasMaxLength(256);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // ดัชนีช่วยค้น
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => new { x.Action, x.CreatedAt });
                e.HasIndex(x => new { x.SubjectType, x.SubjectId });

                // FK ไปผู้ใช้ (ลบผู้ใช้ให้ SetNull เพื่อเก็บล็อกไว้)
                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
