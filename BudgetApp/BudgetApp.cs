using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors();
builder.Services.AddDbContext<BudgetDb>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("BudgetDb")));

var app = builder.Build();

ClaudeBudgetParser.Initialize(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/transaction", async (Transaction transaction, BudgetDb db) =>
{
    db.Transactions.Add(transaction);
    await db.SaveChangesAsync();
    return Results.Ok(new TransactionResponse(transaction.Id, transaction.Amount, transaction.Date, transaction.Category, null));
})
.WithName("CreateTransaction");

app.MapPost("/transaction/parse", async (NaturalLanguageInput input, BudgetDb db) =>
{
    var entry = await ClaudeBudgetParser.ParseBudgetEntryAsync(input.Text);

    if (entry.NeedsClarification != null)
    {
        return Results.Ok(new TransactionResponse(0, 0, default, null, entry.NeedsClarification));
    }

    var transaction = new Transaction(entry.Amount, entry.Date, entry.Category!);

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync();
    return Results.Ok(new TransactionResponse(transaction.Id, transaction.Amount, transaction.Date, transaction.Category, null));
})
.WithName("ParseTransaction");

app.MapGet("/transactions", async (BudgetDb db) =>
{
    var transactions = await db.Transactions.ToListAsync();
    return Results.Ok(transactions);
})
.WithName("GetTransactions");

app.Run();

record Transaction(decimal Amount, string Date, string Category)
{
    public int Id {get; set;}
};

record TransactionResponse(int Id, decimal Amount, string Date, string? Category, string? NeedsClarification);

record NaturalLanguageInput(string Text);
