namespace Finance.Domain;

public static class BalanceCalculator
{
    public static long Calculate(Guid accountId, IEnumerable<Transaction> transactions)
    {
        long balance = 0;
        foreach (var t in transactions.Where(x => x.DeletedAt == null))
        {
            if (t.Type == TransactionType.Income && t.AccountId == accountId) balance += t.Amount;
            else if (t.Type == TransactionType.Expense && t.AccountId == accountId) balance -= t.Amount;
            else if (t.Type == TransactionType.Transfer)
            {
                if (t.AccountId == accountId) balance -= t.Amount;
                if (t.TransferAccountId == accountId) balance += t.Amount;
            }
        }
        return balance;
    }

    public static (long income, long expense, long net) Summary(IEnumerable<Transaction> transactions, DateTime? from = null, DateTime? to = null)
    {
        var filtered = transactions.Where(t => t.DeletedAt == null);
        if (from != null) filtered = filtered.Where(t => t.TransactionDate >= from);
        if (to != null) filtered = filtered.Where(t => t.TransactionDate <= to);

        long income = filtered.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        long expense = filtered.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return (income, expense, income - expense);
    }
}
