using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using BlazorEmailApp.Data;
using BlazorEmailApp.Models;

namespace BlazorEmailApp.Services;

public class EmailService
{
    private readonly AppDbContext _context;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<EmailService> _logger;
    
    public EmailService(AppDbContext context, EncryptionService encryptionService, ILogger<EmailService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }
    
    public async Task<List<EmailAccount>> GetEmailAccountsAsync()
    {
        return await _context.EmailAccounts
            .OrderByDescending(ea => ea.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<EmailAccount?> GetEmailAccountAsync(int id)
    {
        return await _context.EmailAccounts.FindAsync(id);
    }
    
    public async Task<EmailAccount> CreateEmailAccountAsync(EmailAccount account, string plainPassword)
    {
        account.EncryptedPassword = _encryptionService.Encrypt(plainPassword);
        _context.EmailAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }
    
    public async Task<bool> UpdateEmailAccountAsync(EmailAccount account, string? plainPassword = null)
    {
        var existing = await _context.EmailAccounts.FindAsync(account.Id);
        if (existing == null) return false;
        
        existing.Name = account.Name;
        existing.Email = account.Email;
        existing.ImapServer = account.ImapServer;
        existing.ImapPort = account.ImapPort;
        existing.UseSsl = account.UseSsl;
        
        if (!string.IsNullOrEmpty(plainPassword))
        {
            existing.EncryptedPassword = _encryptionService.Encrypt(plainPassword);
        }
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> DeleteEmailAccountAsync(int id)
    {
        var account = await _context.EmailAccounts.FindAsync(id);
        if (account == null) return false;
        
        _context.EmailAccounts.Remove(account);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> TestConnectionAsync(EmailAccount account, string password)
    {
        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(account.ImapServer, account.ImapPort, 
                account.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
            await client.AuthenticateAsync(account.Email, password);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test IMAP connection for {Email}", account.Email);
            return false;
        }
    }
    
    public async Task<List<Email>> GetEmailsAsync(int? accountId = null, int page = 1, int pageSize = 20, string filter = "all")
    {
        var query = _context.Emails.AsQueryable();
        
        if (accountId.HasValue)
        {
            // For now, we'll get all emails. In a real app, you might want to associate emails with accounts
            query = query.Where(e => true); // Placeholder for account filtering
        }
        
        // Apply filter
        switch (filter.ToLower())
        {
            case "unread":
                query = query.Where(e => !e.IsRead);
                break;
            case "read":
                query = query.Where(e => e.IsRead);
                break;
            case "all":
            default:
                // No filter applied
                break;
        }
        
        return await query
            .OrderByDescending(e => e.ReceivedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    public async Task<int> GetEmailsCountAsync(int? accountId = null, string filter = "all")
    {
        var query = _context.Emails.AsQueryable();
        
        if (accountId.HasValue)
        {
            // For now, we'll get all emails. In a real app, you might want to associate emails with accounts
            query = query.Where(e => true); // Placeholder for account filtering
        }
        
        // Apply filter
        switch (filter.ToLower())
        {
            case "unread":
                query = query.Where(e => !e.IsRead);
                break;
            case "read":
                query = query.Where(e => e.IsRead);
                break;
            case "all":
            default:
                // No filter applied
                break;
        }
        
        return await query.CountAsync();
    }
    
    public async Task<Email?> GetEmailAsync(int id)
    {
        return await _context.Emails.FindAsync(id);
    }
    
    public async Task<bool> MarkEmailAsReadAsync(int id)
    {
        var email = await _context.Emails.FindAsync(id);
        if (email == null) return false;
        
        email.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<int> SyncEmailsAsync(int accountId, bool fullSync = false, IProgress<(int current, int total, string status)>? progress = null)
    {
        var account = await _context.EmailAccounts.FindAsync(accountId);
        if (account == null) return 0;
        
        var password = _encryptionService.Decrypt(account.EncryptedPassword);
        var newEmailsCount = 0;
        
        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(account.ImapServer, account.ImapPort, 
                account.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None);
            await client.AuthenticateAsync(account.Email, password);
            
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);
            
            _logger.LogInformation("Syncing emails for account {AccountId}. Total messages in inbox: {Count}", accountId, inbox.Count);
            
            // Get all UIDs in the inbox
            var allUids = await inbox.SearchAsync(SearchQuery.All);
            _logger.LogInformation("Found {Count} total UIDs in inbox", allUids.Count);
            
            if (allUids.Count == 0)
            {
                _logger.LogInformation("No messages found in inbox");
                return 0;
            }
            
            // Determine which UIDs to sync
            var uidsToSync = new List<UniqueId>();
            if (fullSync)
            {
                // Full sync: get all UIDs (but limit to last 1000 to avoid performance issues)
                var startIndex = Math.Max(0, allUids.Count - 1000);
                uidsToSync = allUids.Skip(startIndex).ToList();
                _logger.LogInformation("Performing full sync: {Count} UIDs (from {Start} to end)", uidsToSync.Count, startIndex);
            }
            else
            {
                // Incremental sync: get UIDs newer than the last synced email
                var lastSyncedEmail = await _context.Emails
                    .OrderByDescending(e => e.ReceivedDate)
                    .FirstOrDefaultAsync();
                
                if (lastSyncedEmail != null)
                {
                    // Get UIDs that are newer than the last synced email
                    // Since we can't easily map ReceivedDate back to UID, we'll take a smaller batch
                    // and rely on the duplicate detection
                    uidsToSync = allUids.TakeLast(50).ToList(); // Reduced from 100 to 50
                    _logger.LogInformation("Performing incremental sync: {Count} UIDs (since last sync: {LastSync})", uidsToSync.Count, lastSyncedEmail.ReceivedDate);
                }
                else
                {
                    // No emails in database, do a small initial sync
                    uidsToSync = allUids.TakeLast(50).ToList();
                    _logger.LogInformation("Performing initial incremental sync: {Count} UIDs", uidsToSync.Count);
                }
            }
            
            if (uidsToSync.Count == 0)
            {
                _logger.LogInformation("No UIDs to sync");
                return 0;
            }
            
            // Fetch message summaries using UIDs
            var messages = await inbox.FetchAsync(uidsToSync, MessageSummaryItems.Full);
            _logger.LogInformation("Fetched {Count} message summaries using UIDs", messages.Count);
            
            if (messages.Count == 0)
            {
                _logger.LogInformation("No message summaries returned from fetch");
                return 0;
            }
            
            progress?.Report((0, messages.Count, $"Found {messages.Count} emails to process"));
            
            var processedCount = 0;
            foreach (var message in messages)
            {
                try
                {
                    _logger.LogDebug("Processing message with UniqueId: {UniqueId}, Index: {Index}", message.UniqueId, message.Index);
                    
                    // Validate UniqueId before processing
                    if (message.UniqueId == UniqueId.Invalid)
                    {
                        _logger.LogWarning("Skipping message with invalid UniqueId at index {Index}", message.Index);
                        continue;
                    }
                    
                    var fullMessage = await inbox.GetMessageAsync(message.UniqueId);
                    var messageId = fullMessage.MessageId ?? Guid.NewGuid().ToString();
                    
                    // Check if email already exists using multiple criteria
                    var existingEmail = await _context.Emails.FirstOrDefaultAsync(e => 
                        e.MessageId == messageId || 
                        (e.From == fullMessage.From.ToString() && 
                         e.Subject == (fullMessage.Subject ?? "") && 
                         e.ReceivedDate == fullMessage.Date.DateTime));
                    
                    if (existingEmail != null)
                    {
                        _logger.LogDebug("Email already exists (MessageId: {MessageId}, Subject: {Subject}), skipping", messageId, fullMessage.Subject);
                        continue;
                    }
                    
                    var email = new Email
                    {
                        MessageId = messageId,
                        From = fullMessage.From.ToString(),
                        To = fullMessage.To.ToString(),
                        Subject = fullMessage.Subject ?? "",
                        Content = GetTextContent(fullMessage),
                        ReceivedDate = fullMessage.Date.DateTime,
                        HasAttachments = fullMessage.Attachments.Any(),
                        Snippet = GetSnippet(fullMessage),
                        IsRead = false
                    };
                    
                    _context.Emails.Add(email);
                    newEmailsCount++;
                    
                    processedCount++;
                    progress?.Report((processedCount, messages.Count, $"Processing email {processedCount}/{messages.Count}: {email.Subject}"));
                    
                    _logger.LogInformation("Added email: {Subject} from {From} (UID: {UniqueId})", email.Subject, email.From, message.UniqueId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process message UniqueId: {UniqueId}, Index: {Index}, skipping", message.UniqueId, message.Index);
                    continue;
                }
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully synced {Count} new emails", newEmailsCount);
            
            account.LastSync = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync emails for account {AccountId}", accountId);
        }
        
        return newEmailsCount;
    }
    
    private string GetTextContent(MimeMessage message)
    {
        var textBody = message.TextBody;
        if (!string.IsNullOrEmpty(textBody))
            return textBody;
        
        var htmlBody = message.HtmlBody;
        if (!string.IsNullOrEmpty(htmlBody))
        {
            // Simple HTML to text conversion
            return System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]*>", "");
        }
        
        return "";
    }
    
    private string GetSnippet(MimeMessage message)
    {
        var content = GetTextContent(message);
        return content.Length > 200 ? content.Substring(0, 200) + "..." : content;
    }
} 