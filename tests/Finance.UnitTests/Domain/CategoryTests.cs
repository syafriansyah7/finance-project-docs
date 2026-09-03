using Finance.Domain;

namespace Finance.UnitTests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_expense_succeeds()
    {
        var cat = Category.Create(Guid.NewGuid(), Guid.NewGuid(), "Food", "Expense");
        Assert.Equal("Expense", cat.Kind);
        Assert.False(cat.IsArchived);
    }

    [Fact]
    public void Create_income_succeeds()
    {
        var cat = Category.Create(Guid.NewGuid(), Guid.NewGuid(), "Salary", "Income");
        Assert.Equal("Income", cat.Kind);
    }

    [Fact]
    public void Create_with_invalid_kind_throws()
    {
        Assert.Throws<ArgumentException>(() => Category.Create(Guid.NewGuid(), Guid.NewGuid(), "X", "Transfer"));
    }

    [Fact]
    public void Create_with_empty_name_throws()
    {
        Assert.Throws<ArgumentException>(() => Category.Create(Guid.NewGuid(), Guid.NewGuid(), "", "Expense"));
    }
}
