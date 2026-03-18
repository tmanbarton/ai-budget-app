using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors();
builder.Services.AddDbContext<BudgetDb>(options =>
options.UseSqlite("Data Source=budget.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseHttpsRedirection();

app.MapPost("/transaction", (Transaction transaction, BudgetDb db) =>
{
    db.Transactions.Add(transaction);
    db.SaveChanges();
    return Results.Ok(transaction);
})
.WithName("CreateTransaction");

app.MapGet("/transactions", (BudgetDb db) =>
{
    var transactions = db.Transactions.ToList();
    return Results.Ok(transactions);
})
.WithName("GetTransactions");

app.Run();

record Transaction(decimal Amount, DateOnly Date, string Category)
{
    public int Id {get; set;}
};
