using System.Text;

using Microsoft.Extensions.Logging;

public sealed class Report
{
    private readonly ILoggerFactory _log;
    private readonly string? _link;
    private readonly string _authorEmail;
    private readonly double _rating;
    private readonly uint _earnedXp;
    private readonly uint _xpBeforeNextTaco;
    private readonly uint _earnedTacos;
    private readonly IEnumerable<(double rating, string email)> _leaderboard;

    public Report(
        ILoggerFactory log,
        string? link,
        string authorEmail,
        double rating,
        uint earnedXp,
        uint xpBeforeNextTaco,
        uint earnedTacos,
        IEnumerable<(double rating, string email)> leaderboard
    )
    {
        _log = log;
        _link = link;
        _authorEmail = authorEmail;
        _rating = rating;
        _earnedXp = earnedXp;
        _xpBeforeNextTaco = xpBeforeNextTaco;
        _earnedTacos = earnedTacos;
        _leaderboard = leaderboard;
    }

    public void Write(string path)
    {
        var _content = new StringBuilder();
        _content.AppendLine($"PR: {_link}  ");
        _content.AppendLine($"Author: <{_authorEmail}>  ");
        _content.AppendLine($"Previous Rating: {_rating:F0}  ");
        _content.AppendLine($"XP: +{_earnedXp}  ");
        _content.AppendLine($"XP before next taco: {_xpBeforeNextTaco}  ");

        if (_earnedTacos > 0)
            _content.AppendLine($"🌮 × {_earnedTacos} taco(s) earned  ");

        _content.AppendLine();

        _content.AppendLine("Rating | Author");
        _content.AppendLine("------ | ------");

        foreach (var a in _leaderboard)
            _content.AppendLine($"{a.rating,6:F0} | <{a.email}>");

        File.WriteAllText(path, _content.ToString());
        
        _log.CreateLogger<Report>().LogInformation(
            new EventId(1723245),
            _content.ToString()
        );
    }
}