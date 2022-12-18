using System.Reflection;
using devrating.git;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(
    builder => builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddSystemdConsole());

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation(
    new EventId(1511174),
    Assembly.GetExecutingAssembly().GetName().Version?.ToString());

if (args.Length < 6)
{
    logger.LogInformation(
        new EventId(1342447),
        "Not enough arguments. Probably mergeCommitSha is empty");
    return;
}

var gh = new GitHubClient(
    githubToken: args[1],
    log: loggerFactory,
    workspace: args[2],
    baseBranch: args[3],
    ownerAndRepository: args[0],
    number: int.Parse(args[4]));

var r = new Ratings(loggerFactory);

foreach (var pr in await gh.RecentMergedPrs())
    r.Apply(pr);

await gh.UpdatePrLabels(
    r.Rating(
        new GitDiff(
            log: loggerFactory,
            @base:
                new GitProcess(
                    loggerFactory,
                    "git",
                    $"rev-parse {args[5]}~",
                    args[2])
                .Output()
                .First(),
            commit:
                new GitProcess(
                    loggerFactory,
                    "git",
                    $"rev-parse {args[5]}",
                    args[2])
                .Output()
                .First(),
            since:
                new GitLastMajorUpdateTag(
                    loggerFactory, args[2], args[5])
                .Sha(),
            repository: args[2],
            key: args[0].Split('/')[1],
            link: $"https://github.com/{args[0]}/pull/{int.Parse(args[4])}",
            organization: args[0].Split('/')[0],
            createdAt: DateTimeOffset.UtcNow)));
