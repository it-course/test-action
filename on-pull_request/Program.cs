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
    ownerAndRepository: args[0]);

var m = new StabilityMetric(loggerFactory, args[4]);

foreach (var pr in await gh.RecentMergedPrs())
    if (!m.IsCommitApplied(gh.Owner(), gh.Repository(), pr.Oid))
        m.Apply(
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
