using Microsoft.EntityFrameworkCore;
using McpScheduler.Core.Models;
using McpScheduler.Infrastructure.Data;

namespace McpScheduler.Infrastructure
{
    /// <summary>
    /// Main database context for the application
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class
        /// </summary>
        /// <param name="options">The DB context options</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the conversations DbSet
        /// </summary>
        public DbSet<Conversation> Conversations { get; set; }

        /// <summary>
        /// Gets or sets the response targets DbSet
        /// </summary>
        public DbSet<ResponseTarget> ResponseTargets { get; set; }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Conversation entity
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ScheduledTime).IsRequired();
                entity.Property(c => c.ConversationText).IsRequired();
                entity.Property(c => c.CreatedAt).IsRequired();
                entity.Property(c => c.UpdatedAt).IsRequired();
                entity.Property(c => c.Status).IsRequired();

                // Configure one-to-one relationship with ResponseTarget
                entity.HasOne(c => c.Target)
                    .WithOne()
                    .HasForeignKey<ResponseTarget>("ConversationId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ResponseTarget entity
            modelBuilder.Entity<ResponseTarget>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.ConversationId).IsRequired();
                entity.Property(t => t.Endpoint).IsRequired();
                entity.Property(t => t.Method).IsRequired();
                entity.Property(t => t.HeadersJson);
                entity.Property(t => t.AdditionalInfo);
            });
        }
    }
}
