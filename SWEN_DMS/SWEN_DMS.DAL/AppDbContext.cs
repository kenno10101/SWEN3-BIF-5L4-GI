namespace SWEN_DMS.DAL;
using Microsoft.EntityFrameworkCore;
using SWEN_DMS.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
}
