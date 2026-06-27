using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Entities;

namespace UrlShortener.Api.Persistence;

public class UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options)
    : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.ToTable("short_urls");

            entity.HasKey(shortUrl => shortUrl.Id);

            entity.HasIndex(shortUrl => shortUrl.ShortCode)
                .IsUnique();

            entity.Property(shortUrl => shortUrl.ShortCode)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(shortUrl => shortUrl.LongUrl)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(shortUrl => shortUrl.CreatedAtUtc)
                .IsRequired();
        });
    }
}
