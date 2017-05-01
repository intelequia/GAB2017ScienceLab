using GAB.BatchServer.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GAB.BatchServer.API.Data
{
    /// <summary>
    /// Represents the BatchServer database context
    /// </summary>
    public class BatchServerContext: DbContext
    {
        /// <summary>
        /// Constructor for the BatchServerContext class
        /// </summary>
        /// <param name="options"></param>
        public BatchServerContext(DbContextOptions<BatchServerContext> options) : base(options)
        {            
        }
        /// <summary>
        /// Event fired on model creation
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LabUser>()
                .HasIndex(u => u.EMail)
                .IsUnique()
                .HasName("IDX_Email");
            modelBuilder.Entity<LabUser>()
                .Property(u => u.CreatedOn)
                .HasDefaultValueSql("getutcdate()");
            modelBuilder.Entity<LabUser>()
                .Property(u => u.ModifiedOn)
                .HasDefaultValueSql("getutcdate()");

            modelBuilder.Entity<Input>()
                .HasIndex(i => i.Status)
                .HasName("IDX_Status");
            modelBuilder.Entity<Input>()
                .HasIndex(i => i.BatchId)
                .HasName("IDX_BatchId");
            modelBuilder.Entity<Input>()
                .Property(i => i.CreatedOn)
                .HasDefaultValueSql("getutcdate()");
            modelBuilder.Entity<Input>()
                .Property(i => i.ModifiedOn)
                .HasDefaultValueSql("getutcdate()");

            modelBuilder.Entity<Output>()
                .Property(o => o.CreatedOn)
                .HasDefaultValueSql("getutcdate()");
            modelBuilder.Entity<Output>()
                .Property(o => o.ModifiedOn)
                .HasDefaultValueSql("getutcdate()");

        }

        /// <summary>
        /// Inputs dataset
        /// </summary>
        public DbSet<Input> Inputs { get; set; }
        /// <summary>
        /// Outputs dataset
        /// </summary>
        public DbSet<Output> Outputs { get; set; }
        /// <summary>
        /// Lab users dataset
        /// </summary>
        public DbSet<LabUser> LabUsers { get; set; }
    }
}
