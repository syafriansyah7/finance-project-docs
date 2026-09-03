using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public sealed class CategoryService(FinanceDbContext db)
{
    public async Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken ct)
        => await db.Categories.Where(c => c.UserId == userId && !c.IsArchived).ToListAsync(ct);

    public async Task<Category> CreateAsync(Guid userId, string name, string kind, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(c => c.UserId == userId && c.Name == name.Trim() && c.Kind == kind, ct))
            throw new InvalidOperationException("Category already exists");
        var category = Category.Create(Guid.NewGuid(), userId, name, kind);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task<Category> GetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Category not found");
        return cat;
    }

    public async Task ArchiveAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var cat = await GetAsync(userId, id, ct);
        cat.Archive();
        await db.SaveChangesAsync(ct);
    }
}
