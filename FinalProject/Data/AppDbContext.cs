using FinalProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Data  
{
    public class AppDbContext : DbContext
    {
        // Constructor รับ options แล้วส่งให้ base class
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet ของตารางเบียร์ท้องถิ่น
        public DbSet<LocalBeer> LocalBeers { get; set; }
    }
}
