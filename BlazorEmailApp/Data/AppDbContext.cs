using Microsoft.EntityFrameworkCore;
using BlazorEmailApp.Models;

namespace BlazorEmailApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Email> Emails { get; set; }
    public DbSet<EmailAccount> EmailAccounts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Email entity
        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasIndex(e => e.ReceivedDate);
            entity.HasIndex(e => e.IsRead);
            
            entity.Property(e => e.MessageId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.From).IsRequired().HasMaxLength(500);
            entity.Property(e => e.To).HasMaxLength(500);
            entity.Property(e => e.Subject).HasMaxLength(1000);
            entity.Property(e => e.Content).HasColumnType("TEXT");
            entity.Property(e => e.Snippet).HasMaxLength(500);
        });
        
        // Configure EmailAccount entity
        modelBuilder.Entity<EmailAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImapServer).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EncryptedPassword).IsRequired();
        });
    }
} 