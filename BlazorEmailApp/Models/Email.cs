using System.ComponentModel.DataAnnotations;

namespace BlazorEmailApp.Models;

public class Email
{
    public int Id { get; set; }
    
    [Required]
    public string MessageId { get; set; } = string.Empty;
    
    [Required]
    public string From { get; set; } = string.Empty;
    
    public string To { get; set; } = string.Empty;
    
    public string Subject { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public DateTime ReceivedDate { get; set; }
    
    public bool IsRead { get; set; }
    
    public bool HasAttachments { get; set; }
    
    public string? Snippet { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 