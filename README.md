# 📊 User Activity Logger

> A flexible and comprehensive logging system for tracking user activities across the application with automatic context capture and security-first design.

---

## 🎯 Overview

The User Activity Logger provides seamless tracking of user interactions throughout your ASP.NET Core application. Using declarative attributes and action filters, it automatically captures detailed audit trails without cluttering your business logic.

### ✨ Key Features

- 🔐 **Automatic Security** - Redacts sensitive fields (passwords, secrets)
- 🎯 **Zero Boilerplate** - Simple attribute decoration on controller actions
- 📝 **Rich Context** - Captures HTTP details, user info, IP addresses, and custom data
- ⚡ **Performance Optimized** - Selective logging with configurable verbosity
- 🔄 **Conditional Actions** - Dynamic log descriptions based on operation results
- 🗃️ **Database Backed** - Persistent storage with Entity Framework Core

---

## 🏗️ Architecture

```
Controller Action
      ↓
[LogUserActivity] Attribute
      ↓
UserActivityLoggingFilter (IAsyncResultFilter)
      ↓
UserActivityLogger Service
      ↓
Database (UserLog Entity)
```

### 📦 Components

| Component | Purpose |
|-----------|---------|
| **UserLog** | Entity model for storing log entries |
| **IUserActivityLogger** | Service interface for logging operations |
| **UserActivityLogger** | Implementation with automatic context capture |
| **LogUserActivityAttribute** | Declarative attribute for marking loggable actions |
| **UserActivityLoggingFilter** | Action filter that intercepts and processes requests |

---

## 🚀 Quick Start

### 1️⃣ Database Setup

The `UserActivityLog` entity captures:

| Field | Description |
|-------|-------------|
| `Id` | Primary key |
| `Event` | Action description |
| `UserId` | Foreign key to Employee |
| `ResponseStatusCode` | HTTP status code |
| `IPAddress` | Client IP address |
| `Path` | Full request URL |
| `Method` | HTTP method (GET, POST, etc.) |
| `AdditionalData` | JSON serialized custom data |
| `DateEvent` | Timestamp (defaults to `DateTime.Now`) |

```csharp
 protected override void OnModelCreating(ModelBuilder builder)
 {
     base.OnModelCreating(builder);

     builder.ConfigureUserActivityLogger(); 
 }
 ```

Run your migrations to create the table:
```bash
dotnet ef migrations add AddUserActivityLogging
dotnet ef database update
```

### 2️⃣ Service Registration

Add to `Program.cs`:

```csharp
// Add UserActivityLogger services
builder.Services.AddUserActivityLogger(options =>
{
    options.EnableRequestBodyLogging = false;
    options.LogAnonymousUsers = true;
    options.RedactedFields = new[] { "password", "pwd", "secret" };
    options.SkipPaths = new[] { "/health", "/api/metrics" };
    
    // Custom IP resolver for load balancer/proxy scenarios
    options.IpAddressResolver = (httpContext) =>
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return !string.IsNullOrEmpty(forwardedFor) 
            ? forwardedFor.Split(',')[0].Trim() 
            : httpContext.Connection.RemoteIpAddress?.ToString();
    };
});

// Register the filter with MVC
builder.Services.AddControllersWithViews(options =>
{
    options.AddUserActivityLogging(); // Add this line
});
```

### 3️⃣ Start Logging

Decorate your controller actions:

```csharp
[LogUserActivity(ConstLogUser.ViewAssignments)]
public async Task<IActionResult> Index()
{
    return View(await _assignmentsServicesBL.PrepareListAsync());
}
```

✅ **That's it!** The logger will automatically capture user activity.

---

## 📖 Usage Guide

### 🔹 Basic Logging

Perfect for simple GET requests or actions without sensitive data:

```csharp
[HttpGet]
[LogUserActivity("View Dashboard")]
public async Task<IActionResult> Dashboard()
{
    return View();
}
```

**What gets logged:**
- User ID
- Event: "View Dashboard"
- Request path and method
- IP address
- Response status code
- Timestamp

---

### 🔹 Logging with Request Body

Capture form data for POST/PUT operations (sensitive fields are automatically redacted):

```csharp
[HttpPost]
[LogUserActivity(ConstLogUser.CreateAssignment, LogRequestBody = true)]
public async Task<IActionResult> CreateAssignment(AssignmentVM model)
{
    // Your business logic
    return Ok();
}
```

**Security Features:**
- ✅ Automatically redacts: `password`, `pwd`, `secret` fields
- ✅ Skips anti-forgery tokens (`__RequestVerificationToken`)
- ✅ Filters out DataTables parameters (`columns[]`, `order[]`, etc.)
- ✅ JSON serializes form data with UTF-8 encoding

**Example logged data:**
```json
{
  "assignmentTitle": "Q4 Project Review",
  "assignedTo": "EMP001",
  "password": "***REDACTED***"
}
```

---

### 🔹 Conditional Action Descriptions

For operations with multiple outcomes (activate/deactivate, approve/reject):

```csharp
[HttpPost]
[LogUserActivity(ConstLogUser.DeactivateSites, LogActionArguments = true)]
public async Task<IActionResult> ChangeStatus(int id)
{
    var result = await _sitesServicesBL.ChangeStatusAsync(id, User.GetUserId());
    
    if (result == eChangeStatusResult.Activate)
    {
        // Override the log description dynamically
        HttpContext.Items["ConditionalLogAction"] = ConstLogUser.ActivateSites;
        return Ok(new { body = "Site activated successfully" });
    }
    
    if (result == eChangeStatusResult.Deactivate)
    {
        HttpContext.Items["ConditionalLogAction"] = ConstLogUser.DeactivateSites;
        return Ok(new { body = "Site deactivated successfully" });
    }
    
    return BadRequest(new { body = "Operation failed" });
}
```

**Result:** The log will show the actual operation performed (Activate vs Deactivate) instead of the default description.

---

### 🔹 Manual Logging

For scenarios outside controller actions (background jobs, SignalR hubs, etc.):

```csharp
public class ReportService
{
    private readonly IUserActivityLogger _userLogger;
    
    public ReportService(IUserActivityLogger userLogger)
    {
        _userLogger = userLogger;
    }
    
    public async Task GenerateReport(string reportType)
    {
        // Your logic here...
        
        // Log the activity
        await _userLogger.LogActionAsync(
            "Generate Report", 
            new { ReportType = reportType, GeneratedAt = DateTime.Now }
        );
    }
}
```

**For complete control:**
```csharp
await _userLogger.LogAsync(new UserLog
{
    Event = "Background Job Completed",
    UserId = systemUserId,
    Path = "/jobs/process",
    Method = "BACKGROUND",
    AdditionalData = JsonSerializer.Serialize(jobDetails)
});
```

---

## ⚙️ Configuration Reference

### LogUserActivityAttribute Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ActionDescription` | `string?` | `null` | Human-readable description of the action |
| `LogRequestBody` | `bool` | `false` | Captures form data (auto-redacts sensitive fields) |
| `LogActionArguments` | `bool` | `false` | Enables dynamic action description via `HttpContext.Items` |

### Usage Examples

```csharp
// Minimal - uses action name as description
[LogUserActivity]
public IActionResult Index() => View();

// With description
[LogUserActivity("View Employee List")]
public IActionResult Employees() => View();

// With request body logging
[HttpPost]
[LogUserActivity("Update Profile", LogRequestBody = true)]
public IActionResult UpdateProfile(ProfileVM model) => Ok();

// With conditional logging
[HttpPost]
[LogUserActivity("Change Status", LogActionArguments = true)]
public IActionResult Toggle(int id)
{
    // Set HttpContext.Items["ConditionalLogAction"] based on outcome
    return Ok();
}
```

---

## 🛡️ Security & Best Practices

### ✅ DO

- **Use constants** for action descriptions to ensure consistency:
  ```csharp
  public static class ConstLogUser
  {
      public const string ViewAssignments = "View Assignments";
      public const string CreateAssignment = "Create Assignment";
      public const string ActivateSites = "Activate Site";
  }
  ```

- **Be selective** with `LogRequestBody = true` - only use for critical operations
- **Review logs regularly** for security auditing and compliance
- **Implement retention policies** to manage database size

### ❌ DON'T

- ❌ Log every single action (causes performance overhead and noise)
- ❌ Manually handle sensitive data redaction (it's automatic)
- ❌ Store logs indefinitely without archiving strategy
- ❌ Log to production without testing first

---

## 🔍 Querying Logs

### Recent User Activity
```csharp
var recentActivity = await _context.UserLogs
    .Include(l => l.User)
    .Where(l => l.UserId == currentUserId)
    .OrderByDescending(l => l.DateEvent)
    .Take(50)
    .ToListAsync();
```

### Search by Event Type
```csharp
var auditTrail = await _context.UserLogs
    .Where(l => l.Event.Contains("Delete") || l.Event.Contains("Deactivate"))
    .OrderByDescending(l => l.DateEvent)
    .ToListAsync();
```

### Failed Operations (4xx/5xx status codes)
```csharp
var failures = await _context.UserLogs
    .Where(l => l.ResponseStatusCode >= 400)
    .Include(l => l.User)
    .OrderByDescending(l => l.DateEvent)
    .Take(100)
    .ToListAsync();
```

### Activity by IP Address
```csharp
var suspiciousActivity = await _context.UserLogs
    .Where(l => l.IPAddress == "192.168.1.100")
    .OrderByDescending(l => l.DateEvent)
    .ToListAsync();
```

---

## 🐛 Troubleshooting

### ❓ Logs Not Being Created

**Check:**
1. ✅ Filter is registered globally in `Program.cs`
2. ✅ Attribute is applied to controller action
3. ✅ `IHttpContextAccessor` is registered
4. ✅ Database connection is valid
5. ✅ Migrations have been applied

**Debug tip:**
```csharp
// Add logging in UserActivityLogger constructor
public UserActivityLogger(...)
{
    _logger.LogInformation("UserActivityLogger initialized");
}
```

### ❓ Request Body Not Captured

**Possible causes:**
- `LogRequestBody = false` (check attribute settings)
- Request is not form-encoded (must be `application/x-www-form-urlencoded` or `multipart/form-data`)
- Fields are being filtered (DataTables params, anti-forgery tokens)

**Test with:**
```csharp
[HttpPost]
[LogUserActivity("Test", LogRequestBody = true)]
public IActionResult Test([FromForm] string testField)
{
    return Ok(new { received = testField });
}
```

### ❓ User ID Is NULL

**Check:**
1. User is authenticated (`User.Identity.IsAuthenticated`)
2. `ClaimTypes.NameIdentifier` exists in claims
3. Authentication middleware is configured properly

**Verify claims:**
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
_logger.LogInformation($"Current User ID: {userId ?? "NULL"}");
```

---

## 🎨 Example Dashboard Query

Build an activity dashboard with this query:

```csharp
public async Task<ActivityDashboardVM> GetDashboardData(DateTime fromDate)
{
    var logs = await _context.UserLogs
        .Include(l => l.User)
        .Where(l => l.DateEvent >= fromDate)
        .ToListAsync();
    
    return new ActivityDashboardVM
    {
        TotalActions = logs.Count,
        UniqueUsers = logs.Select(l => l.UserId).Distinct().Count(),
        MostActiveUser = logs.GroupBy(l => l.UserId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First().User.Name)
            .FirstOrDefault(),
        TopActions = logs.GroupBy(l => l.Event)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToList(),
        FailureRate = logs.Count(l => l.ResponseStatusCode >= 400) * 100.0 / logs.Count
    };
}
```

---

## 🚀 Future Enhancements

Consider implementing:

- 📨 **Background Logging** - Queue-based logging for high-traffic scenarios (using Hangfire/MassTransit)
- 📊 **Analytics Dashboard** - Visual reporting of user activities and trends
- 🗄️ **Auto-Archiving** - Move old logs to cold storage (e.g., Azure Blob Storage)
- 📤 **Export Functionality** - CSV/Excel export for compliance audits
- 🔌 **External Integration** - Send logs to Serilog, ELK Stack, or Application Insights
- 🔔 **Alerting** - Real-time notifications for suspicious activities

---

## 📞 Support

For questions or issues:
1. Check this documentation
2. Review existing logs for patterns
3. Contact the development team

**Version:** 1.0.9  
**Last Updated:** 2025  
**Maintained by:** Asma El-Hadiedy

---
  
**Built with ❤️ for better application monitoring and security**
