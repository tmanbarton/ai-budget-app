using System.Text;
using System.Text.Json;

public class BudgetEntry
{
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Category { get; set; }
    public string? NeedsClarification {get; set; }
}

public class ClaudeBudgetParser
{
    private static readonly HttpClient _client = new HttpClient();

    public static void Initialize(string apiKey)
    {
        _client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _client.DefaultRequestHeaders.Add("anthropic-version", "2024-06-01");
    }

    public static async Task<BudgetEntry> ParseBudgetEntryAsync(string userInput)
    {
        // 1. Build the request body as an anonymous object
        var requestBody = new
        {
            model = "claude-sonnet-4-5",
            max_tokens = 1024,
            system = "You are an assistant that extracts structured budget entry information from user input. The user will provide a description of a financial transaction and TODO.",
            messages = new[]
            {
                new { role = "user", content = userInput }
            },
            output_config = new
            {
                format = new
                {
                    type = "json_object",
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