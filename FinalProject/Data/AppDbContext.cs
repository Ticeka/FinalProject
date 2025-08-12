using FinalProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<LocalBeer> LocalBeers { get; set; } = default!;
        public DbSet<QuickRating> QuickRatings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // QuickRating config
            modelBuilder.Entity<QuickRating>(e =>
            {
                // 1 device ต่อ 1 เครื่องดื่ม ให้ได้ 1 แถว (กดซ้ำคืออัปเดต)
                e.HasIndex(x => new { x.LocalBeerId, x.DeviceId }).IsUnique();

                e.Property(x => x.DeviceId).HasMaxLength(64).IsRequired();
                e.Property(x => x.IpHash).HasMaxLength(128).IsRequired();

                // คะแนนต้องอยู่ระหว่าง 1–5
                e.ToTable(tb => tb.HasCheckConstraint("CK_QuickRatings_Score", "[Score] BETWEEN 1 AND 5"));

                // (ทางเลือก) ให้ DB ใส่เวลาสร้างเป็น UTC อัตโนมัติ
                // e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            });
        }
    }
}
