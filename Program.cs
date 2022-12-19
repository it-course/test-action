using System.Reflection;
using devrating.git;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(
    builder => builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddSystemdConsole());

loggerFactory.CreateLogger<Program>().LogInformation(
    new EventId(1511174),
    Assembly.GetExecutingAssembly().GetName().Version?.ToString());

var gh = new GitHubClient(
    githubToken: args[1],
    logger: loggerFactory,
    workspace: args[2],
    baseBranch: args[3],
    ownerAndRepository: args[0],
    prNumber: int.Parse(args[4]));

var r = new Ratings(loggerFactory);

foreach (var pr in await gh.RecentMergedPrs())
    if (!r.IsCommitApplied(gh.Owner(), gh.Repository(), pr.Oid))
        r.Apply(
            new GitDiff(
                log: loggerFactory,
                @base:
                    new GitProcess(
                        loggerFactory,
                        "git",
                        $"rev-parse {pr.Oid}~",
                        gh.Workspace())
                    .Output()
                    .First(),
                commit:
                    new GitProcess(
                        loggerFactory,
                        "git",
                        $"rev-parse {pr.Oid}",
                        gh.Workspace())
                    .Output()
                    .First(),
                since:
                    new GitLastMajorUpdateTag(
                        loggerFactory,
                        gh.Workspace(),
                        pr.Oid)
                    .Sha(),
                repository: gh.Workspace(),
                key: gh.Repository(),
                link: pr.Url,
                organization: gh.Owner(),
                createdAt: pr.MergedAt!.Value));

if (!bool.Parse(args[7]))
    await gh.UpdatePrLabels(
        r.Rating(
            new GitDiff(
                log: loggerFactory,
                @base:
                    new GitProcess(
                        loggerFactory,
                        "git",
                        $"rev-parse {args[5]}",
                        gh.Workspace())
                    .Output()
                    .First(),
                commit:
                    new GitProcess(
                        loggerFactory,
                        "git",
                        $"rev-parse {args[6]}",
                        gh.Workspace())
                    .Output()
                    .First(),
                since:
                    new GitLastMajorUpdateTag(
                        loggerFactory, gh.Workspace(), args[6])
                    .Sha(),
                repository: gh.Workspace(),
                key: gh.Repository(),
                link: $"https://github.com/{gh.OwnerAndRepository()}/pull/{gh.PrNumber()}",
                organization: gh.Owner(),
                createdAt: DateTimeOffset.UtcNow)));
