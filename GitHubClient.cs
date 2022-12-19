using System.Reflection;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

public sealed class GitHubClient
{
    private readonly Octokit.GraphQL.IConnection github;
    private readonly IGitHubClient githubRest;
    private readonly string githubToken;
    private readonly ILoggerFactory loggerFactory;
    private readonly string workspace;
    private readonly string baseBranch;
    private readonly string ownerAndRepository;
    private readonly int number;
    private readonly string owner;
    private readonly string repository;
    private readonly SizeLabel sizeLabel;
    private readonly StabilityLabel stabilityLabel;

    public GitHubClient(
        string githubToken,
        ILoggerFactory log,
        string workspace,
        string baseBranch,
        string ownerAndRepository,
        int prNumber)
    {
        this.github = new Octokit.GraphQL.Connection(
            new Octokit.GraphQL.ProductHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version!.ToString()),
            githubToken);
        this.githubToken = githubToken;
        this.loggerFactory = log;
        this.workspace = workspace;
        this.baseBranch = baseBranch;
        this.ownerAndRepository = ownerAndRepository;
        this.number = prNumber;
        this.owner = ownerAndRepository.Split('/')[0];
        this.repository = ownerAndRepository.Split('/')[1];
        this.githubRest = new Octokit.GitHubClient(
            new Octokit.ProductHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version!.ToString()))
        {
            Credentials = new Octokit.Credentials(githubToken)
        };
        this.sizeLabel = new SizeLabel(githubRest, owner, repository, prNumber);
        this.stabilityLabel = new StabilityLabel(githubRest, owner, repository, prNumber);
    }

    public string Workspace() => workspace;
    public string Owner() => owner;
    public string Repository() => repository;
    public string OwnerAndRepository() => ownerAndRepository;
    public int PrNumber() => number;

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

    public async Task UpdatePrLabels(double rating)
    {
        await sizeLabel.Update();
        await stabilityLabel.Update(rating);
    }

    public record PullRequestRecord(string Oid, DateTimeOffset? MergedAt, string Url);
}