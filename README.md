# Blazor Email App

A modern email client built with **Blazor** that allows you to read emails from IMAP accounts with a clean, responsive web interface. It's a perfect base for your future email-based projects.

## Features

### **Email Account Management**
- **Secure Configuration**: Add and manage multiple IMAP email accounts
- **Password Encryption**: All passwords are encrypted using AES encryption

### **Email Synchronization**
- **Full Sync**: Download all emails from your IMAP account
- **Incremental Sync**: Download only new emails since last sync
- **Real-time Progress**: Visual progress tracking during sync operations
- **Duplicate Prevention**: Smart duplicate detection to avoid re-downloading emails

### **Email Management**
- **Email List**: View all synced emails with pagination
- **Read/Unread Filtering**: Filter emails by read status
- **Email Details**: Full email content viewing
- **Auto-read Marking**: Emails are automatically marked as read when viewed

### **Modern UI**
- **Responsive Design**: Works on desktop, tablet, and mobile
- **Tailwind CSS**: Modern, clean styling
- **Interactive Components**: Real-time updates and smooth interactions
- **Progress Indicators**: Visual feedback for all operations

## Technology Stack
- **.NET 9** - Latest .NET framework
- **Blazor Server** - Interactive web UI with C#
- **Entity Framework Core** - Database ORM
- **SQLite** - Local database storage
- **MailKit** - IMAP email library
- **Tailwind CSS** - Modern CSS framework
- **Bootstrap** - Additional UI components

## Prerequisites

- **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **IMAP Account** - Any email provider that supports IMAP (Gmail, Outlook, Yahoo, etc.)

## Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/pj-os/blazor-imap.git
cd blazor-imap/BlazorEmailApp
```

### 2. Run the Application
```bash
dotnet run
```

### 3. Open in Browser
Navigate to `http://localhost:5184` (or the URL shown in the terminal)

### 4. Configure Your Email Account
1. Click **"Configure Accounts"** in the navigation
2. Click **"Add Account"**
3. Enter your IMAP settings:
   - **Account Name**: Any name you prefer
   - **Email Address**: Your email address
   - **IMAP Server**: Your provider's IMAP server (e.g., `imap.gmail.com`)
   - **Port**: Usually 993 for SSL or 143 for non-SSL
   - **Use SSL**: Check this for most providers
   - **Password**: Your email password
4. Click **"Add Account"**

### 5. Sync Your Emails
1. Go to the **"Emails"** page
2. Click **"Full Sync"** to download all emails
3. Or click **"Sync New"** for incremental updates

## Configuration

### Database
The app uses SQLite for local storage. The database file (`BlazorEmailApp.db`) is created automatically and excluded from Git.

### Encryption
Email passwords are encrypted using AES encryption. The encryption key is stored in `appsettings.json`.

### Development Settings
- Development-specific settings are in `appsettings.Development.json`
- This file is excluded from Git for security

Thank you! Happy coding.
