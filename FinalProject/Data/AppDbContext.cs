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

            modelBuilder.Entity<QuickRating>(e =>
            {
                e.ToTable("QuickRatings");
                e.HasKey(x => x.Id);

                e.Property(x => x.Score).IsRequired();
                e.Property(x => x.IpHash).HasMaxLength(64);
                e.Property(x => x.Fingerprint).HasMaxLength(128);

                e.HasOne(x => x.LocalBeer)
                 .WithMany()                 // ถ้าอยากให้ LocalBeer มี ICollection<QuickRating> ค่อยเปลี่ยนเป็น .WithMany(b => b.QuickRatings)
                 .HasForeignKey(x => x.LocalBeerId)
                 .OnDelete(DeleteBehavior.Cascade);

                // ถ้าต้องการกันโหวตซ้ำระดับ IP ต่อหนึ่งเบียร์ ให้ทำ index นี้ (ไม่ต้อง unique ก็ได้)
                e.HasIndex(x => new { x.LocalBeerId, x.IpHash });
            });
        }
    }
}
