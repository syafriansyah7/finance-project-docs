using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
        });

        b.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Type).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.UserId, x.Name, x.Kind }).IsUnique();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Kind).IsRequired();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.TransactionDate });
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.CategoryId);
            e.Property(x => x.Amount).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().IsRequired();
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SyncOperation>(e =>
        {
            e.HasKey(x => x.OperationId);
            e.HasIndex(x => new { x.UserId, x.ServerVersion });
            e.Property(x => x.EntityType).IsRequired();
            e.Property(x => x.OperationType).IsRequired();
        });

        b.Entity<Budget>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.CategoryId, x.Month }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Month });
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
