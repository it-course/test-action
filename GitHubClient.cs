using System.Reflection;
using devrating.factory;
using devrating.git;
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
        int number)
    {
        this.github = new Octokit.GraphQL.Connection(
            new Octokit.GraphQL.ProductHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version!.ToString()),
            githubToken);
        this.loggerFactory = log;
        this.workspace = workspace;
        this.baseBranch = baseBranch;
        this.owner = ownerAndRepository.Split('/')[0];
        this.repository = ownerAndRepository.Split('/')[1];
        this.githubRest = new Octokit.GitHubClient(
            new Octokit.ProductHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version!.ToString()))
        {
            Credentials = new Octokit.Credentials(githubToken)
        };
        this.sizeLabel = new SizeLabel(githubRest, owner, repository, number);
        this.stabilityLabel = new StabilityLabel(githubRest, owner, repository, number);
    }

    public async Task<IEnumerable<Diff>> RecentMergedPrs()
    {
        var q = $"repo:{owner}/{repository} base:{baseBranch} type:pr merged:>{DateTimeOffset.UtcNow.AddDays(-90):O} sort:updated-desc";

        loggerFactory.CreateLogger<Program>().LogInformation(
            new EventId(1880477),
            $"Last merged PRs search query: `{q}`");

        return await github.Run(
            new Query()
                .Search(
                    q,
                    SearchType.Issue,
                    100)
                .Nodes
                .OfType<Octokit.GraphQL.Model.PullRequest>()
                .Select(
                    pr => new GitDiff(
                        loggerFactory,
                        new GitProcess(loggerFactory, "git", $"rev-parse {pr.MergeCommit.Oid}~", workspace).Output().First(),
                        new GitProcess(loggerFactory, "git", $"rev-parse {pr.MergeCommit.Oid}", workspace).Output().First(),
                        new GitLastMajorUpdateTag(loggerFactory, workspace, pr.MergeCommit.Oid).Sha(),
                        workspace,
                        repository,
                        pr.Url,
                        owner,
                        pr.MergedAt!.Value))
                .Compile());
    }

    public async Task UpdatePrLabels(double rating)
    {
        await sizeLabel.Update();
        await stabilityLabel.Update(rating);
    }
}