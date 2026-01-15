namespace SWEN_DMS.DAL;

using Microsoft.EntityFrameworkCore;
using SWEN_DMS.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentNote> DocumentNotes => Set<DocumentNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DocumentNote>(entity =>
        {
            entity.ToTable("DocumentNotes");

            // PK
            entity.HasKey(n => n.Id);

            // Required fields
            entity.Property(n => n.Content)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(n => n.CreatedAtUtc)
                .IsRequired();

            // FK: DocumentId -> Documents(Id), cascade delete
            entity.HasOne(n => n.Document)
                .WithMany(d => d.Notes)
                .HasForeignKey(n => n.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for faster lookups
            entity.HasIndex(n => n.DocumentId);
        });
    }
}