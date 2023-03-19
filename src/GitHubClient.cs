using System.Reflection;

using Microsoft.Extensions.Logging;

using Octokit.GraphQL;
using Octokit.GraphQL.Model;

public sealed class GitHubClient
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _githubToken;
    private readonly string _baseBranch;
    private readonly string _owner;
    private readonly string _repository;

    public string Owner() => _owner;
    public string Repository() => _repository;

    public GitHubClient(
        string githubToken,
        ILoggerFactory log,
        string baseBranch,
        string ownerAndRepository)
    {
        _githubToken = githubToken;
        _loggerFactory = log;
        _baseBranch = baseBranch;
        _owner = ownerAndRepository.Split('/')[0];
        _repository = ownerAndRepository.Split('/')[1];
    }

    public async Task<IEnumerable<PullRequestRecord>> RecentMergedPrs(
        DateTimeOffset mergedAfter
    )
    {
        var q = $"repo:{_owner}/{_repository} base:{_baseBranch} type:pr " +
            $"merged:>{mergedAfter:O} sort:updated-desc";

        _loggerFactory.CreateLogger<GitHubClient>().LogInformation(
            new EventId(1880477),
            "Last merged PRs search query: `{q}`",
            q
        );

        var r = (
            await new Octokit.GraphQL.Connection(
                new Octokit.GraphQL.ProductHeaderValue(
                    Assembly.GetExecutingAssembly().GetName().Name,
                    Assembly.GetExecutingAssembly().GetName().Version!.ToString()),
                _githubToken)
            .Run(
                new Query()
                .Search(
                    q,
                    SearchType.Issue,
                    100)
                .Nodes
                .OfType<Octokit.GraphQL.Model.PullRequest>()
                .Select(
                    pr => new PullRequestRecord(
                        pr.MergeCommit.Oid,
                        pr.MergedAt,
                        pr.Url)
                )
                .Compile()
            )
        )
        .OrderBy(pr => pr.MergedAt!)
        .ToArray();

        _loggerFactory.CreateLogger<GitHubClient>().LogInformation(
            new EventId(1380246),
            "Last merged PRs count: `{count}`",
            r.Length
        );

        return r;
    }

    public record PullRequestRecord(string Oid, DateTimeOffset? MergedAt, string Url);
}