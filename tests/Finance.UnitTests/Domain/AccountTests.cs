using Finance.Domain;

namespace Finance.UnitTests.Domain;

public class AccountTests
{
    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var account = Account.Create(Guid.NewGuid(), Guid.NewGuid(), "Cash", "Cash", "IDR");
        Assert.Equal("Cash", account.Name);
        Assert.Equal("IDR", account.Currency);
        Assert.False(account.IsArchived);
    }

    [Fact]
    public void Create_with_invalid_type_throws()
    {
        Assert.Throws<ArgumentException>(() => Account.Create(Guid.NewGuid(), Guid.NewGuid(), "X", "Invalid"));
    }

    [Fact]
    public void Create_with_empty_name_throws()
    {
        Assert.Throws<ArgumentException>(() => Account.Create(Guid.NewGuid(), Guid.NewGuid(), " ", "Cash"));
    }

    [Fact]
    public void Create_with_bad_currency_throws()
    {
        Assert.Throws<ArgumentException>(() => Account.Create(Guid.NewGuid(), Guid.NewGuid(), "Cash", "Cash", "ID"));
    }

    [Fact]
    public void Archive_marks_archived()
    {
        var account = Account.Create(Guid.NewGuid(), Guid.NewGuid(), "Bank", "Bank");
        account.Archive();
        Assert.True(account.IsArchived);
    }
}
