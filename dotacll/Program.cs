using System.Text.Json.Serialization;
using System;
using System.IO; 
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HttpClient client = new HttpClient();
// Головний URL API
string baseUrl = "https://api.opendota.com/api";
// Якщо є API-ключ, вставте його сюди, наприклад: "?api_key=your_key"
var config = new ConfigurationBuilder()
    // Використовуємо Path.Combine для точного склеювання шляху до папки з назвою файлу
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: true)
    .Build();

string? rawKey = config["OpenDota:ApiKey"];
string apiKey = !string.IsNullOrEmpty(rawKey) ? $"?api_key={rawKey}" : "";

// перевірте, чи є цей запуск зверху, або просто використайте повний шлях Path.Combine




// ОБОВ'ЯЗКОВО: OpenDota вимагає наявність User-Agent
client.DefaultRequestHeaders.Add("User-Agent", "CSharp-OpenDota-App");
Console.WriteLine($"{baseUrl}/heroes{apiKey}");
try 
{
    
    Console.WriteLine("--- Отримуємо список героїв ---");
    await GetHeroesExampleAsync(client, baseUrl, apiKey);

    Console.WriteLine("\n--- Останні матчі гравця ---");
    long accountId = 1136968788; // Тестовий Steam32 ID
    await GetPlayerRecentMatchesAsync(client, baseUrl, accountId, apiKey);
}
catch (Exception ex)
{
    Console.WriteLine($"Сталася помилка: {ex.Message}");
}

// --- МЕТОДИ ДЛЯ ЗАПИТІВ ---

async Task GetHeroesExampleAsync(HttpClient client, string url, string apiKey)
{
    // Робимо GET-запит та одразу десеріалізуємо в List<Hero>
    var heroes = await client.GetFromJsonAsync<List<Hero>>($"{url}/heroes{apiKey}");

    if (heroes != null)
    {   
        Console.WriteLine($"Усього знайдено героїв: {heroes.Count}");
        
        // Виведемо перших 3 героїв для перевірки
        for (int i = 0; i < Math.Min(3, heroes.Count); i++)
        {
            var h = heroes[i];
            Console.WriteLine($"ID: {h.Id} | Ім'я: {h.LocalizedName} | Атрибут: {h.PrimaryAttr}");
        }
    }
}

async Task GetPlayerRecentMatchesAsync(HttpClient client, string url, long accountId, string apiKey)
{
    var matches = await client.GetFromJsonAsync<List<RecentMatch>>($"{url}/players/{accountId}/recentMatches{apiKey}");

    if (matches != null)
    {
        // Беремо перші 5 матчів
        for (int i = 0; i < Math.Min(5, matches.Count); i++)
            {
                var m = matches[i];
                int durationMinutes = m.Duration / 60;
                Console.WriteLine($"Матч ID: {m.MatchId} | KDA: {m.Kills}/{m.Deaths}/{m.Assists} | Тривалість: {durationMinutes} хв");
            }
    }
    else
    {
        Console.WriteLine("Не вдалося знайти матчі або профіль приватний.");
    }
}

public class Hero
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("localized_name")]
    public string LocalizedName { get; set; } = string.Empty;

    [JsonPropertyName("primary_attr")]
    public string PrimaryAttr { get; set; } = string.Empty;
}

public class RecentMatch
{
    [JsonPropertyName("match_id")]
    public long MatchId { get; set; }

    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; } // в секундах
}
