using System.Reflection;

using devrating.factory;
using devrating.git;

using Microsoft.Extensions.Logging;

var lf = LoggerFactory.Create(
    builder => builder
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning)
    .AddConsole()
);

var log = lf.CreateLogger<Program>();

log.LogInformation(
    new EventId(1511174),
    Assembly.GetExecutingAssembly().GetName().Version?.ToString()
);

var gh = new GitHubClient(
    githubToken: args[1],
    log: lf,
    baseBranch: args[3],
    ownerAndRepository: args[0]
);

var sm = new RatingSystem(
    log: lf,
    formula: new DefaultFormula(),
    database: args[4]
);

var sha = new GitProcess(
    log: lf,
    filename: "git",
    arguments: new[]
    {
        "rev-parse",
        args[5],
    },
    directory: args[2]
)
.Output()
.First();

foreach (
    var pr in await gh.RecentMergedPrs(
        mergedAfter: sm.LastWorkCreatedAt(
            organization: gh.Owner(),
            repository: gh.Repository()
        ) ?? DateTimeOffset.Parse(args[7])
    )
)
{
    log.LogInformation(
        new EventId(1325093),
        "Analyzing PR: `{PR}`",
        pr.Url
    );

    var oid = new GitProcess(
        log: lf,
        filename: "git",
        arguments: new[] {
            "rev-parse",
            pr.Oid,
        },
        directory: args[2]
    )
    .Output()
    .First();

    var d = new GitDiff(
        log: lf,
        @base: new GitProcess(
            log: lf,
            filename: "git",
            arguments: new[] {
                "rev-parse",
                $"{pr.Oid}~",
            },
            directory: args[2]
        )
        .Output()
        .First(),
        commit: oid,
        since: new GitLastMajorUpdateTag(
            loggerFactory: lf,
            repository: args[2],
            before: pr.Oid
        )
        .Sha(),
        repository: args[2],
        key: gh.Repository(),
        link: pr.Url,
        organization: gh.Owner(),
        createdAt: pr.MergedAt!.Value,
        paths: args[10..]
    );

    if (d.Additions() < uint.Parse(args[6]))
    {
        log.LogInformation(
            new EventId(1770471),
            "Skipping too small PR: `{PR}`",
            pr.Url
        );

        continue;
    }

    sm.Apply(diff: d);

    if (sha.Equals(oid))
        await sm.GiveTaco(
            diff: d,
            xpPerTaco: uint.Parse(args[9]),
            heyTacoClient: new HeyTacoClient(
                token: args[8],
                log: lf
            )
        );
}