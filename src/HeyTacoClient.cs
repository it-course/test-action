using System.Net.Http.Headers;
using System.Text.Json;

using Microsoft.Extensions.Logging;

public sealed class HeyTacoClient
{
    private readonly string _token;
    private readonly ILoggerFactory _log;

    public HeyTacoClient(string token, ILoggerFactory log)
    {
        _token = token;
        _log = log;
    }

    public async Task GiveTaco(string email, uint amount)
    {
        _log.CreateLogger<HeyTacoClient>().LogInformation(
            new EventId(1885735),
            "Giving `{amount}` taco(s) to `{email}`",
            amount,
            email
        );

        var content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    token = _token,
                    uid = MapToUid(email),
                    amount = amount,
                    message = "Thanks for all your hard work!",
                }
            ),
            new MediaTypeHeaderValue("application/json")
        );

        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await new HttpClient().PostAsync("https://www.heytaco.chat/api/app.giveTaco", content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to give taco");

        var json = await response.Content.ReadAsStringAsync();

        _log.CreateLogger<HeyTacoClient>().LogInformation(
            new EventId(1885737),
            "Response: {json}",
            json
        );

        var result = JsonSerializer.Deserialize<GiveTacoResult>(json);

        if (result == null)
            throw new Exception("Failed to deserialize response");

        if (!result.ok)
            throw new Exception(result.error);
    }

    private string MapToUid(string email) => email[0..email.IndexOf('@')];
}

internal sealed record GiveTacoResult(bool ok, string error, string info);
