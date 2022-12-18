using devrating.entity;
using devrating.factory;
using devrating.sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

public sealed class Ratings
{
    private readonly ILoggerFactory loggerFactory;
    private readonly Formula formula = new DefaultFormula();
    private readonly Database database =
        new SqliteDatabase(
            new TransactedDbConnection(
                new SqliteConnection("Data Source=devrating.sqlite3")));

    public Ratings(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public void Apply(Diff diff)
    {
        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation(
            new EventId(1461486),
            $"Applying diff: `{diff.ToJson()}`");

        database.Instance().Connection().Open();

        using var transaction = database.Instance().Connection().BeginTransaction();

        try
        {
            if (!database.Instance().Present())
            {
                database.Instance().Create();
            }

            if (diff.PresentIn(database.Entities().Works()))
            {
                logger.LogInformation(
                    new EventId(1435402),
                    $"The diff is already applied. Skipping");
                return;
            }

            NewWork(diff);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
        finally
        {
            database.Instance().Connection().Close();
        }
    }

    public double Rating(Diff diff)
    {
        database.Instance().Connection().Open();

        using var transaction = database.Instance().Connection().BeginTransaction();

        try
        {
            if (!database.Instance().Present())
            {
                database.Instance().Create();
            }

            Work w;

            if (!diff.PresentIn(database.Entities().Works()))
            {
                w = NewWork(diff);
            }
            else
            {
                w = diff.RelatedWork(database.Entities().Works());
            }

            return database
                .Entities()
                .Ratings()
                .GetOperation()
                .RatingsOf(w.Id())
                .First(r => r
                    .Author()
                    .Id()
                    .Equals(w.Author().Id()))
                .Value();
        }
        finally
        {
            transaction.Rollback();
            database.Instance().Connection().Close();
        }
    }

    private Work NewWork(Diff diff)
    {
        var authorFactory = new DefaultAuthorFactory(database.Entities().Authors());
        var ratings = database.Entities().Ratings();

        return diff.NewWork(
            new DefaultFactories(
                authorFactory,
                new DefaultWorkFactory(
                    database.Entities().Works(),
                    ratings,
                    authorFactory),
                new DefaultRatingFactory(
                    loggerFactory,
                    authorFactory,
                    ratings,
                    formula)));
    }
}