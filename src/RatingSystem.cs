using devrating.entity;
using devrating.factory;
using devrating.sqlite;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

public sealed class RatingSystem
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Formula _formula;
    private readonly Database _database;

    public RatingSystem(ILoggerFactory log, Formula formula, string database)
    {
        _loggerFactory = log;
        _formula = formula;
        _database = new SqliteDatabase(
            new TransactedDbConnection(
                new SqliteConnection($"Data Source='{database}'")
            )
        );
    }

    public DateTimeOffset? LastWorkCreatedAt(string organization, string repository)
    {
        var logger = _loggerFactory.CreateLogger<RatingSystem>();
        _database.Instance().Connection().Open();

        using var transaction = _database.Instance().Connection().BeginTransaction();

        try
        {
            if (!_database.Instance().Present())
                return null;

            return _database.Entities()
            .Works()
            .GetOperation()
            .Last(
                organization: organization,
                repository: repository,
                after: DateTimeOffset.MinValue
            )
            .FirstOrDefault()
            ?.CreatedAt();
        }
        finally
        {
            transaction.Rollback();
            _database.Instance().Connection().Close();
        }
    }

    public void Apply(Diff diff)
    {
        var logger = _loggerFactory.CreateLogger<RatingSystem>();

        logger.LogInformation(
            new EventId(1461486),
            $"Applying diff: `{diff.ToJson()}`");

        _database.Instance().Connection().Open();

        using var transaction = _database.Instance().Connection().BeginTransaction();

        try
        {
            if (!_database.Instance().Present())
            {
                logger.LogInformation(
                    new EventId(1824416),
                    $"The DB is not present. Creating");

                _database.Instance().Create();
            }

            var authorFactory = new DefaultAuthorFactory(_database.Entities().Authors());
            var ratings = _database.Entities().Ratings();

            diff.NewWork(
               new DefaultFactories(
                   authorFactory,
                   new DefaultWorkFactory(
                       _database.Entities().Works(),
                       ratings,
                       authorFactory),
                   new DefaultRatingFactory(
                       _loggerFactory,
                       authorFactory,
                       ratings,
                       _formula
                   )
               )
            );

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
        finally
        {
            _database.Instance().Connection().Close();
        }
    }

    public Report Report(Diff diff, uint xpPerTaco)
    {
        var log = _loggerFactory.CreateLogger<RatingSystem>();

        log.LogInformation(new EventId(1146241), $"Diff: `{diff.ToJson()}`");

        _database.Instance().Connection().Open();

        using var transaction = _database.Instance().Connection().BeginTransaction();

        try
        {
            var experience = new Experience(_formula);
            var w = diff.RelatedWork(_database.Entities().Works());
            var a = w.Author();
            var ur = w.UsedRating();
            var r = ur.Id().Filled() ? ur.Value() : _formula.DefaultRating();
            var xp = _database
            .Entities()
            .Works()
            .GetOperation()
            .Last(author: a.Id(), after: DateTimeOffset.MinValue)
            .Sum(
                (work) =>
                {
                    var wur = work.UsedRating();
                    return experience
                    .Gain(
                        rating: wur
                        .Id()
                        .Filled()
                        ? wur.Value()
                        : _formula.DefaultRating()
                    );
                }
            );

            var earnedXp = experience.Gain(rating: r);
            var tacosBefore = (xp - earnedXp) / xpPerTaco;
            var tacosAfter = xp / xpPerTaco;

            return new Report(
                log: _loggerFactory,
                link: w.Link(),
                authorEmail: a.Email(),
                rating: r,
                earnedXp: earnedXp,
                xpBeforeNextTaco: (uint)(xpPerTaco - (xp % xpPerTaco)),
                earnedTacos: (uint)(tacosAfter - tacosBefore),
                leaderboard: _database
                .Entities()
                .Authors()
                .GetOperation()
                .Top(
                    a.Organization(),
                    a.Repository(),
                    DateTimeOffset.MinValue
                )
                .Select(
                    (author) => (
                        rating: _database
                        .Entities()
                        .Ratings()
                        .GetOperation()
                        .RatingOf(author.Id())
                        .Value(),
                        email: author.Email()
                    )
                )
                .ToList()
            );
        }
        finally
        {
            transaction.Rollback();
            _database.Instance().Connection().Close();
        }
    }
}