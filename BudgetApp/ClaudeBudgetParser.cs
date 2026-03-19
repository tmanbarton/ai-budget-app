using System.Text;
using System.Text.Json;

public class BudgetEntry
{
    public decimal Amount { get; set; }
    public string? Date { get; set; }
    public string? Category { get; set; }
    public string? NeedsClarification {get; set; }
}

public class ClaudeBudgetParser
{
    private static readonly HttpClient _client = new HttpClient();

    public static void Initialize(string apiKey)
    {
        _client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public static async Task<BudgetEntry> ParseBudgetEntryAsync(string userInput)
    {
        // 1. Build the request body as an anonymous object
        var requestBody = new
        {
            model = "claude-sonnet-4-5",
            max_tokens = 1024,
            system = $"""
            You are an assistant that extracts structured budget entry information from user input. The user will provide a description of a financial transaction that will include the amount of the transaction, the date of the transaction, and a store or item or some other way you need to derive the category out of a specific list of categories.
            The categories you can use are: Groceries, Dining, Housing, Entertainment, Car Stuff, Debt, Healthcare, Donations, Other. NEVER make up a category that isn't on this list.
            If a transaction seems like it can fit into multiple of these categories or it isn't clear which one, ask the user to clarify which category it belongs to, e.g. User: "I spent $25 on Amazon yesterday" This could be either "Entertainment" or "Other" category, so you should respond with "NeedsClarification": "Does the transaction belong in Entertainment or Other category?" and leave the category field null.
            If the transaction clearly fits into one of the categories, fill out the category field and set NeedsClarification to null.
            Today's date is {DateTime.Now:MM/dd/yyyy}.
            If the user doesn't provide a date, assume the transaction happened today and set the date field to today's date in MM/dd/yyyy format.
            """,
            messages = new[]
            {
                new { role = "user", content = userInput }
            },
            output_config = new
            {
                format = new
                {
                    type = "json_schema",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            amount = new { type = new[] { "number", "null" } },
                            date = new { type = new[] { "string", "null" } },
                            category = new { type = new[] { "string", "null" } },
                            needsClarification = new { type = new[] { "string", "null" } }
                        },
                        required = new[] { "amount", "date", "category", "needsClarification" },
                        additionalProperties = false
                    }
                }
            }
        };

        // Serialize the request body to JSON and create the StringContent for the HTTP request
        var json = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Send the POST request to the Claude API
        var response = await _client.PostAsync("https://api.anthropic.com/v1/messages", httpContent);
        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Claude API response: " + responseJson);

        // Parse response JSON to get the assistant's message
        using var document = JsonDocument.Parse(responseJson);
        var assistantMessage = document.RootElement
        .GetProperty("content")[0]
        .GetProperty("text")
        .GetString();

        // Deserialize the assistant's message into a BudgetEntry object
        // var budgetEntry = JsonSerializer.Deserialize<BudgetEntry>(assistantMessage);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var budgetEntry = JsonSerializer.Deserialize<BudgetEntry>(assistantMessage!, options);
        return budgetEntry;
    }
}