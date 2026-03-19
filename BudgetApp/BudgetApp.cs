using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors();
builder.Services.AddDbContext<BudgetDb>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("BudgetDb")));

var app = builder.Build();

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
    if (transaction.Amount <= 0)
        return Results.BadRequest("Think positively. The amount must be greater than 0.");

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync();
    return Results.Ok(transaction);
})
.WithName("CreateTransaction");

app.MapPost("/transaction/parse", async (NaturalLanguageInput input, BudgetDb db) =>
{
    var entry = await ClaudeBudgetParser.ParseBudgetEntryAsync(input.Text);

    if (entry.NeedsClarification != null)
    {
        return Results.BadRequest(entry.NeedsClarification);
    }

    var transaction = new Transaction(entry.Amount, entry.Date, entry.Category!);

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync();
    return Results.Ok(transaction);
})
.WithName("ParseTransaction");

app.MapGet("/transactions", async (BudgetDb db) =>
{
    var transactions = await db.Transactions.ToListAsync();
    return Results.Ok(transactions);
})
.WithName("GetTransactions");

app.Run();

record Transaction(decimal Amount, DateOnly Date, string Category)
{
    public int Id {get; set;}
};

record NaturalLanguageInput(string Text);
