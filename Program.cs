
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class Program
{
    private const string Url = "https://git.toptal.com/screeners/calories-json/-/raw/main/calories.json";

    public static async Task Main(string[] args)
    {
        try
        {
            using var http = new HttpClient();
            // Optional: increase timeout for slow networks
            http.Timeout = TimeSpan.FromSeconds(30);

            Console.WriteLine("Downloading JSON...");
            var json = await http.GetStringAsync(Url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // Show full details on errors while testing
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            // Register our converters to be robust against "number-or-string" fields
            options.Converters.Add(new StringOrNumberToLongConverter());
            options.Converters.Add(new StringToBoolConverter());

            // The JSON is an array of entries
            List<FoodEntry>? entries = JsonSerializer.Deserialize<List<FoodEntry>>(json, options);

            if (entries == null || entries.Count == 0)
            {
                Console.WriteLine("No data found in JSON.");
                return;
            }

            // ------------------------------
            // 1) Highest protein on 2022-11-01
            // ------------------------------
            var targetDate = new DateTime(2022, 11, 1);

            var highProteinName = entries
                .Where(e => e.DateConsumed.Date == targetDate)
                .OrderByDescending(e => e.Protein)
                .Select(e => e.Name)
                .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine("Highest protein meal on 2022-11-01:");
            Console.WriteLine(highProteinName is null ? "(none found)" : highProteinName);

            // ------------------------------
            // 2) Calories > 30 per month: count & total
            // ------------------------------
            var caloriesByMonth = entries
                .Where(e => e.Calories > 30)
                .GroupBy(e => e.DateConsumed.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count(),
                    TotalCalories = g.Sum(x => x.Calories)
                })
                .OrderBy(x => x.Month)
                .ToList();

            Console.WriteLine();
            Console.WriteLine("Calories > 30 grouped by month (count, total):");
            foreach (var row in caloriesByMonth)
            {
                Console.WriteLine($"{row.Month}: Count={row.Count}, TotalCalories={row.TotalCalories}");
            }

            // ------------------------------
            // 3) A couple of extra examples (handy in interviews)
            // ------------------------------
            // Top 3 protein meals overall
            var top3Protein = entries.OrderByDescending(e => e.Protein).Take(3);
            Console.WriteLine();
            Console.WriteLine("Top 3 protein meals overall:");
            foreach (var e in top3Protein)
                Console.WriteLine($"- {e.Name} (Protein={e.Protein}, Date={e.DateConsumed:yyyy-MM-dd})");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error downloading JSON: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex}");
        }
    }
}

/// <summary>
/// Matches the structure of entries in calories.json
/// </summary>
public class FoodEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    // In some sources these can be strings; converters make this robust.
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long UserId { get; set; }

    [JsonPropertyName("age")]
    [JsonConverter(typeof(StringOrNumberToLongConverter))]
    public long Age { get; set; }

    [JsonPropertyName("user_weight")]
    public string? UserWeight { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("weight")]
    public long Weight { get; set; }

    [JsonPropertyName("calories")]
    public long Calories { get; set; }

    [JsonPropertyName("fat")]
    public double Fat { get; set; }

    [JsonPropertyName("carbs")]
    public double Carbs { get; set; }

    [JsonPropertyName("protein")]
    public double Protein { get; set; }

    [JsonPropertyName("time_consumed")]
    public string? TimeConsumed { get; set; }

    [JsonPropertyName("date_consumed")]
    public DateTime DateConsumed { get; set; } // e.g., "2022-11-01"

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("favorite")]
    [JsonConverter(typeof(StringToBoolConverter))]
    public bool Favorite { get; set; }

    [JsonPropertyName("procedence")]
    public string? Procedence { get; set; }
}

/// <summary>
/// Accepts numbers that may come as JSON numbers or strings, and parses them into long.
/// </summary>
public sealed class StringOrNumberToLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetInt64(out var n) ? n : (long)reader.GetDouble(),
            JsonTokenType.String => long.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s) ? s : 0L,
            _ => 0L
        };
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

/// <summary>
/// Accepts booleans that may come as "true"/"false"/"1"/"0" (string) or actual bool.
/// </summary>
public sealed class StringToBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.TryGetInt64(out var n) && n != 0,
            JsonTokenType.String => ParseStringBool(reader.GetString()),
            _ => false
        };

        static bool ParseStringBool(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            return s is "true" or "1" or "yes" or "y";
        }
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}