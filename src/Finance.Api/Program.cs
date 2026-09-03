using System.Security.Claims;
using System.Text;
using Finance.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("Default");
var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "dev-signing-key-must-be-32-chars-long!!";

builder.Services.AddDbContext<FinanceDbContext>(o =>
{
    if (!string.IsNullOrEmpty(conn) && conn.Contains("Host="))
        o.UseNpgsql(conn);
    else
        o.UseInMemoryDatabase("finance_dev");
});

builder.Services.AddScoped<AuthService>(sp => new AuthService(sp.GetRequiredService<FinanceDbContext>(), jwtKey));
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<BudgetService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await db.Database.EnsureCreatedAsync();
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.EnsureSyafriAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/v1/auth/syafri", async (AuthService auth, CancellationToken ct) =>
{
    var user = await auth.EnsureSyafriAsync(ct);
    var token = auth.TokenFor(user);
    return Results.Ok(new { accessToken = token, email = user.Email });
});

app.MapPost("/api/v1/auth/register", async (RegisterRequest req, AuthService auth, CancellationToken ct) =>
{
    var user = await auth.RegisterAsync(req.Email, req.Password, ct);
    return Results.Created($"/api/v1/users/{user.Id}", new { user.Id, user.Email });
});

app.MapPost("/api/v1/auth/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var (user, token, expiresAt) = await auth.LoginAsync(req.Email, req.Password, ct);
        return Results.Ok(new { accessToken = token, expiresAt });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = new { code = "unauthorized", message = ex.Message } }, statusCode: 401);
    }
});

app.MapGet("/api/v1/accounts", async (ClaimsPrincipal user, AccountService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var list = await svc.ListAsync(userId, ct);
    return Results.Ok(list.Select(a => new { a.Id, a.Name, a.Type, a.Currency, a.IsArchived }));
}).RequireAuthorization();

app.MapPost("/api/v1/accounts", async (ClaimsPrincipal user, CreateAccountRequest req, AccountService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var acc = await svc.CreateAsync(userId, req.Name, req.Type, req.Currency ?? "IDR", ct);
        return Results.Created($"/api/v1/accounts/{acc.Id}", new { acc.Id, acc.Name, acc.Type, acc.Currency });
    }
    catch (ArgumentException ex) { return Results.Json(new { error = new { code = "validation_error", message = ex.Message } }, statusCode: 422); }
}).RequireAuthorization();

app.MapGet("/api/v1/accounts/{id:guid}", async (Guid id, ClaimsPrincipal user, AccountService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try { var a = await svc.GetAsync(userId, id, ct); return Results.Ok(new { a.Id, a.Name, a.Type, a.Currency, a.IsArchived }); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapPut("/api/v1/accounts/{id:guid}", async (Guid id, UpdateAccountRequest req, ClaimsPrincipal user, AccountService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var a = await svc.GetAsync(userId, id, ct);
        if (!string.IsNullOrWhiteSpace(req.Name)) a.Rename(req.Name);
        await svc.GetAsync(userId, id, ct);
        return Results.Ok(new { a.Id, a.Name });
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (ArgumentException ex) { return Results.Json(new { error = new { code = "validation_error", message = ex.Message } }, statusCode: 422); }
}).RequireAuthorization();

app.MapDelete("/api/v1/accounts/{id:guid}", async (Guid id, ClaimsPrincipal user, AccountService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try { await svc.ArchiveAsync(userId, id, ct); return Results.NoContent(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapGet("/api/v1/categories", async (ClaimsPrincipal user, CategoryService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var list = await svc.ListAsync(userId, ct);
    return Results.Ok(list.Select(c => new { c.Id, c.Name, c.Kind, c.IsArchived }));
}).RequireAuthorization();

app.MapPost("/api/v1/categories", async (ClaimsPrincipal user, CreateCategoryRequest req, CategoryService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var cat = await svc.CreateAsync(userId, req.Name, req.Kind, ct);
        return Results.Created($"/api/v1/categories/{cat.Id}", new { cat.Id, cat.Name, cat.Kind });
    }
    catch (ArgumentException ex) { return Results.Json(new { error = new { code = "validation_error", message = ex.Message } }, statusCode: 422); }
    catch (InvalidOperationException ex) { return Results.Json(new { error = new { code = "conflict", message = ex.Message } }, statusCode: 409); }
}).RequireAuthorization();

app.MapPut("/api/v1/categories/{id:guid}", async (Guid id, UpdateCategoryRequest req, ClaimsPrincipal user, CategoryService svc, FinanceDbContext db, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var cat = await svc.GetAsync(userId, id, ct);
        if (!string.IsNullOrWhiteSpace(req.Name)) cat.Rename(req.Name);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { cat.Id, cat.Name, cat.Kind });
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapDelete("/api/v1/categories/{id:guid}", async (Guid id, ClaimsPrincipal user, CategoryService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try { await svc.ArchiveAsync(userId, id, ct); return Results.NoContent(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapGet("/api/v1/transactions", async (ClaimsPrincipal user, Guid? accountId, Guid? categoryId, string? type, DateTime? from, DateTime? to, int page, int pageSize, TransactionService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    Finance.Domain.TransactionType? t = type != null ? Enum.Parse<Finance.Domain.TransactionType>(type, true) : null;
    var list = await svc.ListAsync(userId, accountId, categoryId, t, from, to, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, ct);
    return Results.Ok(list.Select(x => new { x.Id, x.Type, x.AccountId, x.TransferAccountId, x.CategoryId, x.Amount, x.Currency, x.Description, x.TransactionDate }));
}).RequireAuthorization();

app.MapPost("/api/v1/transactions", async (ClaimsPrincipal user, CreateTransactionRequest req, TransactionService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var type = Enum.Parse<Finance.Domain.TransactionType>(req.Type, true);
        var tx = await svc.CreateAsync(userId, type, req.AccountId, req.TransferAccountId, req.CategoryId, req.Amount, req.Currency ?? "IDR", req.Description, req.TransactionDate ?? DateTime.UtcNow, req.Id, ct);
        return Results.Created($"/api/v1/transactions/{tx.Id}", new { tx.Id, tx.Type, tx.Amount });
    }
    catch (ArgumentException ex) { return Results.Json(new { error = new { code = "validation_error", message = ex.Message } }, statusCode: 422); }
    catch (KeyNotFoundException ex) { return Results.Json(new { error = new { code = "not_found", message = ex.Message } }, statusCode: 404); }
}).RequireAuthorization();

app.MapGet("/api/v1/transactions/{id:guid}", async (Guid id, ClaimsPrincipal user, TransactionService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try { var t = await svc.GetAsync(userId, id, ct); return Results.Ok(new { t.Id, t.Type, t.Amount, t.AccountId, t.CategoryId, t.TransferAccountId }); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapDelete("/api/v1/transactions/{id:guid}", async (Guid id, ClaimsPrincipal user, TransactionService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try { await svc.DeleteAsync(userId, id, ct); return Results.NoContent(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapGet("/api/v1/accounts/{id:guid}/balance", async (Guid id, ClaimsPrincipal user, TransactionService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var bal = await svc.BalanceAsync(userId, id, ct);
    return Results.Ok(new { accountId = id, balance = bal });
}).RequireAuthorization();

app.MapGet("/api/v1/dashboard/summary", async (ClaimsPrincipal user, string? month, string? period, DashboardService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var p = period ?? month;
    DateTime m;
    if (string.IsNullOrEmpty(p)) m = DateTime.UtcNow;
    else if (p.Length == 7) m = DateTime.Parse(p + "-25");
    else m = DateTime.Parse(p);
    var s = await svc.SummaryAsync(userId, m, ct);
    return Results.Ok(new
    {
        s.Month,
        s.Income,
        s.Expense,
        s.Net,
        accounts = s.Accounts,
        categoryExpenses = s.CategoryExpenses,
        recentTransactions = s.RecentTransactions.Select(t => new { t.Id, t.Type, t.Amount, t.AccountId, t.TransactionDate })
    });
}).RequireAuthorization();

app.MapGet("/api/v1/budgets", async (ClaimsPrincipal user, string? month, string? period, BudgetService svc, FinanceDbContext db, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var p = period ?? month;
    DateTime m;
    if (string.IsNullOrEmpty(p)) m = DateTime.UtcNow;
    else if (p.Length == 7) m = DateTime.Parse(p + "-25");
    else m = DateTime.Parse(p);
    var budgets = await svc.ListAsync(userId, m, ct);
    var txs = await db.Transactions.Where(t => t.UserId == userId && t.DeletedAt == null).ToListAsync(ct);
    return Results.Ok(budgets.Select(b =>
    {
        var (spent, remaining, pct) = b.Progress(txs);
        return new { b.Id, b.CategoryId, b.Month, b.Amount, b.Currency, spent, remaining, pct };
    }));
}).RequireAuthorization();

app.MapPost("/api/v1/budgets", async (ClaimsPrincipal user, CreateBudgetRequest req, BudgetService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var period = req.Month.Length == 7 ? req.Month + "-25" : req.Month;
        var b = await svc.UpsertAsync(userId, req.CategoryId, DateTime.Parse(period), req.Amount, req.Currency ?? "IDR", ct);
        return Results.Created($"/api/v1/budgets/{b.Id}", new { b.Id, b.Amount });
    }
    catch (KeyNotFoundException ex) { return Results.Json(new { error = new { code = "not_found", message = ex.Message } }, statusCode: 404); }
    catch (ArgumentException ex) { return Results.Json(new { error = new { code = "validation_error", message = ex.Message } }, statusCode: 422); }
}).RequireAuthorization();

app.MapDelete("/api/v1/budgets/{id:guid}", async (Guid id, ClaimsPrincipal user, BudgetService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try { await svc.DeleteAsync(userId, id, ct); return Results.NoContent(); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
}).RequireAuthorization();

app.MapPost("/api/v1/sync/push", async (ClaimsPrincipal user, SyncPushRequest req, SyncService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var items = req.Items.Select(i => new SyncPushItem(i.OperationId, i.Entity, i.EntityId, i.Operation, i.ClientUpdatedAt, i.Payload ?? new())).ToList();
    var (results, nextCursor) = await svc.PushAsync(userId, items, ct);
    return Results.Ok(new { results, nextCursor = nextCursor.ToString() });
}).RequireAuthorization();

app.MapGet("/api/v1/sync/pull", async (ClaimsPrincipal user, string? cursor, SyncService svc, CancellationToken ct) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    long cur = 0;
    if (!string.IsNullOrEmpty(cursor)) long.TryParse(cursor, out cur);
    var (next, changes) = await svc.PullAsync(userId, cur, ct);
    return Results.Ok(new { cursor = next.ToString(), changes });
}).RequireAuthorization();

app.Run();

record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);
record CreateAccountRequest(string Name, string Type, string? Currency);
record UpdateAccountRequest(string? Name);
record CreateCategoryRequest(string Name, string Kind);
record UpdateCategoryRequest(string? Name);
record CreateTransactionRequest(Guid? Id, string Type, Guid AccountId, Guid? TransferAccountId, Guid? CategoryId, long Amount, string? Currency, string? Description, DateTime? TransactionDate);
record SyncPushRequestItem(Guid OperationId, string Entity, Guid EntityId, string Operation, DateTime ClientUpdatedAt, Dictionary<string, object?>? Payload);
record SyncPushRequest(List<SyncPushRequestItem> Items);
record CreateBudgetRequest(Guid CategoryId, string Month, long Amount, string? Currency);

public partial class Program { }
