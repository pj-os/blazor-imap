using System.ComponentModel.DataAnnotations;

namespace BlazorEmailApp.Models;

public class EmailAccount
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string ImapServer { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int ImapPort { get; set; }
    
    public string EncryptedPassword { get; set; } = string.Empty;
    
    public bool UseSsl { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSync { get; set; }
} 