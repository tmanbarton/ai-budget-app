using System.Transactions;
using Microsoft.EntityFrameworkCore;

class BudgetDb: DbContext
{
    public BudgetDb(DbContextOptions<BudgetDb> options): base(options) {}
    public DbSet<Transaction> Transactions => Set<Transaction>();
}