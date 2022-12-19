using System.Reflection;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

public sealed class GitHubClient
{
    private readonly Octokit.GraphQL.IConnection github;
    private readonly IGitHubClient githubRest;
    private readonly ILoggerFactory loggerFactory;
    private readonly string workspace;
    private readonly string baseBranch;
    private readonly string ownerAndRepository;
    private readonly string owner;
    private readonly string repository;

    public GitHubClient(
        string githubToken,
        ILoggerFactory logger,
        string workspace,
        string baseBranch,
        string ownerAndRepository)
    {
        this.github = new Octokit.GraphQL.Connection(
            new Octokit.GraphQL.ProductHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version!.ToString()),
            githubToken);
        this.loggerFactory = logger;
        this.workspace = workspace;
        this.baseBranch = baseBranch;
        this.ownerAndRepository = ownerAndRepository;
        this.owner = ownerAndRepository.Split('/')[0];
        this.repository = ownerAndRepository.Split('/')[1];
        this.githubRest = new Octokit.GitHubClient(
            new Octokit.ProductHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version!.ToString()))
        {
            Credentials = new Octokit.Credentials(githubToken)
        };
    }

    public string Workspace() => workspace;
    public string Owner() => owner;
    public string Repository() => repository;
    public string OwnerAndRepository() => ownerAndRepository;

    public async Task<IEnumerable<PullRequestRecord>> RecentMergedPrs()
    {
        var q = $"repo:{owner}/{repository} base:{baseBranch} type:pr merged:>{DateTimeOffset.UtcNow.AddDays(-90):O} sort:updated-desc";

        loggerFactory.CreateLogger<GitHubClient>().LogInformation(
            new EventId(1880477),
            $"Last merged PRs search query: `{q}`");

        return (
            await github.Run(
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
                        pr.Url))
                .Compile()))
        .Reverse();
    }

    public async Task UpdatePrLabels(
        double rating,
        int prNumber)
    {
        await new SizeLabel(githubRest, owner, repository, prNumber).Update();
        await new StabilityLabel(githubRest, owner, repository, prNumber).Update(rating);
    }

    public record PullRequestRecord(string Oid, DateTimeOffset? MergedAt, string Url);
}