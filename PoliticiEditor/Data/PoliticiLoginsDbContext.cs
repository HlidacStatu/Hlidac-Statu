using Microsoft.EntityFrameworkCore;

namespace PoliticiEditor.Data;

public class PoliticiLoginsDbContext(DbContextOptions<PoliticiLoginsDbContext> options)
    : DbContext(options)
{
    public DbSet<PoliticiEditorUser> PoliticiEditorUsers { get; set; }
    public DbSet<PoliticiLoginToken> PoliticiLoginTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PoliticiEditorUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NameId).IsRequired();
            entity.HasIndex(e => e.NameId).IsUnique();
            entity.HasIndex(e => e.EmailUpper);
        });

        modelBuilder.Entity<PoliticiLoginToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
        });
    }
}