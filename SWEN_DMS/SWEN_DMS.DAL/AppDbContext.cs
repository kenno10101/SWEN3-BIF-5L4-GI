namespace SWEN_DMS.DAL;

using Microsoft.EntityFrameworkCore;
using SWEN_DMS.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentNote> DocumentNotes => Set<DocumentNote>();
    public DbSet<AccessLogDaily> AccessLogsDaily => Set<AccessLogDaily>();

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
        
        modelBuilder.Entity<AccessLogDaily>(entity =>
        {
            entity.ToTable("AccessLogsDaily");

            // PK
            entity.HasKey(x => x.Id);

            // DayUtc as DATE in Postgres
            entity.Property(x => x.DayUtc)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(x => x.AccessCount)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            // FK: DocumentId -> Documents(Id)
            entity.HasOne(x => x.Document)
                .WithMany() 
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // pro Dokument & Tag nur 1 Datensatz
            entity.HasIndex(x => new { x.DocumentId, x.DayUtc })
                .IsUnique();

            // schneller lookup pro doc
            entity.HasIndex(x => x.DocumentId);
        });

    }
}