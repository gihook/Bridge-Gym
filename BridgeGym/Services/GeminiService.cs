using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BridgeGym.Models.Bridge;

namespace BridgeGym.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _parseHandImagePrompt;
    private readonly string _parseBoardDiagramPrompt;
    private readonly string _parseBoardDiagramsPrompt;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey =
            configuration["GeminiApiKey"]
            ?? throw new ArgumentNullException("GeminiApiKey is not configured.");
        var model = configuration["GeminiModel"] ?? "gemini-flash-latest";
        var apiVersion = configuration["GeminiApiVersion"] ?? "v1beta";
        _baseUrl =
            $"https://generativelanguage.googleapis.com/{apiVersion}/models/{model}:generateContent";

        var promptPath = Path.Combine(AppContext.BaseDirectory, "ConfigurationPrompts");
        _parseHandImagePrompt = File.ReadAllText(
            Path.Combine(promptPath, "ParseHandImagePrompt.txt")
        );
        _parseBoardDiagramPrompt = File.ReadAllText(
            Path.Combine(promptPath, "ParseBoardDiagramPrompt.txt")
        );
        _parseBoardDiagramsPrompt = File.ReadAllText(
            Path.Combine(promptPath, "ParseBoardDiagramsPrompt.txt")
        );
    }

    public async Task<List<Card>?> ParseHandImageAsync(Stream imageStream)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var base64Image = Convert.ToBase64String(ms.ToArray());

        var cardSchema = new
        {
            type = "object",
            properties = new
            {
                Suit = new
                {
                    type = "string",
                    @enum = new[] { "Spades", "Hearts", "Diamonds", "Clubs" },
                },
                Rank = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "Two",
                        "Three",
                        "Four",
                        "Five",
                        "Six",
                        "Seven",
                        "Eight",
                        "Nine",
                        "Ten",
                        "Jack",
                        "Queen",
                        "King",
                        "Ace",
                    },
                },
            },
            required = new[] { "Suit", "Rank" },
        };

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = _parseHandImagePrompt },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } },
                    },
                },
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                response_schema = new { type = "array", items = cardSchema },
            },
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}?key={_apiKey}", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

        var textResult = geminiResponse?.Candidates?[0].Content?.Parts?[0].Text;

        if (string.IsNullOrEmpty(textResult))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            };
            return JsonSerializer.Deserialize<List<Card>>(textResult, options);
        }
        catch
        {
            return null;
        }
    }

    public async Task<BoardDiagramParseResult?> ParseBoardDiagramAsync(Stream imageStream)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var base64Image = Convert.ToBase64String(ms.ToArray());

        var cardSchema = new
        {
            type = "object",
            properties = new
            {
                Suit = new
                {
                    type = "string",
                    @enum = new[] { "Spades", "Hearts", "Diamonds", "Clubs" },
                },
                Rank = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "Two",
                        "Three",
                        "Four",
                        "Five",
                        "Six",
                        "Seven",
                        "Eight",
                        "Nine",
                        "Ten",
                        "Jack",
                        "Queen",
                        "King",
                        "Ace",
                    },
                },
            },
            required = new[] { "Suit", "Rank" },
        };

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = _parseBoardDiagramPrompt },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } },
                    },
                },
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                response_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        BoardNumber = new { type = "integer" },
                        North = new { type = "array", items = cardSchema },
                        South = new { type = "array", items = cardSchema },
                        East = new { type = "array", items = cardSchema },
                        West = new { type = "array", items = cardSchema },
                    },
                    required = new[] { "BoardNumber", "North", "South", "East", "West" },
                },
            },
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}?key={_apiKey}", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

        var textResult = geminiResponse?.Candidates?[0].Content?.Parts?[0].Text;

        if (string.IsNullOrEmpty(textResult))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            };
            return JsonSerializer.Deserialize<BoardDiagramParseResult>(textResult, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Gemini response: {ex.Message}");
            return null;
        }
    }

    public async Task<List<BoardDiagramParseResult>?> ParseBoardDiagramsAsync(
        IEnumerable<Stream> imageStreams
    )
    {
        var cardSchema = new
        {
            type = "object",
            properties = new
            {
                Suit = new
                {
                    type = "string",
                    @enum = new[] { "Spades", "Hearts", "Diamonds", "Clubs" },
                },
                Rank = new
                {
                    type = "string",
                    @enum = new[]
                    {
                        "Two",
                        "Three",
                        "Four",
                        "Five",
                        "Six",
                        "Seven",
                        "Eight",
                        "Nine",
                        "Ten",
                        "Jack",
                        "Queen",
                        "King",
                        "Ace",
                    },
                },
            },
            required = new[] { "Suit", "Rank" },
        };

        var boardSchema = new
        {
            type = "object",
            properties = new
            {
                BoardNumber = new { type = "integer" },
                North = new { type = "array", items = cardSchema },
                South = new { type = "array", items = cardSchema },
                East = new { type = "array", items = cardSchema },
                West = new { type = "array", items = cardSchema },
            },
            required = new[] { "BoardNumber", "North", "South", "East", "West" },
        };

        var parts = new List<object>();
        parts.Add(new { text = _parseBoardDiagramsPrompt });

        foreach (var imageStream in imageStreams)
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            parts.Add(
                new
                {
                    inline_data = new
                    {
                        mime_type = "image/jpeg",
                        data = Convert.ToBase64String(ms.ToArray()),
                    },
                }
            );
        }

        var requestBody = new
        {
            contents = new[] { new { parts = parts.ToArray() } },
            generationConfig = new
            {
                response_mime_type = "application/json",
                response_schema = new { type = "array", items = boardSchema },
            },
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}?key={_apiKey}", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

        var textResult = geminiResponse?.Candidates?[0].Content?.Parts?[0].Text;

        if (string.IsNullOrEmpty(textResult))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            };
            return JsonSerializer.Deserialize<List<BoardDiagramParseResult>>(textResult, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Gemini response: {ex.Message}");
            throw; // Re-throw to fail the job
        }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
