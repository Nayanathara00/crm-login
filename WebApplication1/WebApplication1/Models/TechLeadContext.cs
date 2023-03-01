using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class TechLeadContext : DbContext
    {
        public TechLeadContext(DbContextOptions<TechLeadContext> options) : base(options)
        {

        }
        public DbSet<TechLead> TechLeads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TechLead>().ToTable("techleads");
        }
    }
}

