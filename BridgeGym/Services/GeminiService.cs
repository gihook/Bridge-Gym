using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BridgeGym.Models.Bridge;
using Microsoft.Extensions.Configuration;

namespace BridgeGym.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GeminiApiKey"] ?? throw new ArgumentNullException("GeminiApiKey is not configured.");
        var model = configuration["GeminiModel"] ?? "gemini-flash-latest";
        var apiVersion = configuration["GeminiApiVersion"] ?? "v1beta";
        _baseUrl = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{model}:generateContent";
    }

    public async Task<List<Card>?> ParseHandImageAsync(byte[] imageBytes)
    {
        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Identify the 13 bridge cards in this image. Return the result strictly as a JSON array of objects, each with 'Suit' and 'Rank' properties. Suits should be 'Spades', 'Hearts', 'Diamonds', 'Clubs'. Ranks should be 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten', 'Jack', 'Queen', 'King', 'Ace'. Do not include any other text or formatting." },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                    }
                }
            }
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

        if (string.IsNullOrEmpty(textResult)) return null;

        // Clean up markdown if present
        if (textResult.StartsWith("```json"))
        {
            textResult = textResult.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (textResult.StartsWith("```"))
        {
            textResult = textResult.Replace("```", "").Trim();
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<List<Card>>(textResult, options);
        }
        catch
        {
            return null;
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
