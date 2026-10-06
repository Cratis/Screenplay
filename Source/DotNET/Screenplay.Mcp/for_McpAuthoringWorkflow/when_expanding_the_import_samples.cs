// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_the_import_samples : given.an_authoring_connection
{
    readonly List<string> _workflowFailures = [];
    readonly List<string> _identityFailures = [];
    readonly List<string> _commentFailures = [];
    readonly List<string> _reachabilityFailures = [];
    readonly List<string> _sliceDocumentFailures = [];
    string _samples = null!;

    void Establish()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, "Samples"))) repository = repository.Parent;
        _samples = Path.Combine(repository!.FullName, "Samples");
    }

    void Because()
    {
        foreach (var sample in new[] { "Commerce", "TimeTracking" })
        {
            var source = Path.Combine(_samples, sample);
            var output = Path.Combine(RootPath, sample);
            foreach (var path in Directory.EnumerateFiles(source, "*.play", SearchOption.AllDirectories))
            {
                var target = Path.Combine(output, Path.GetRelativePath(source, path));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(path, target);
            }

            Root = new(output);
            Connection = new(new McpTools(Root));
            Initialize();
            var initial = Workspace();
            var entries = WorkspaceSyntaxIndex.Create(initial).Entries.Where(entry => entry.Address is not null).ToArray();
            var addresses = entries.Select(entry => entry.Address!).Distinct().ToArray();
            var catalog = SemanticIdentityCatalog.Create(
                initial.IdentityCatalog.Application,
                initial.IdentityCatalog.Documents,
                [.. addresses.Select((address, index) => new SemanticIdentityAssignment(address, SemanticId.Create(SemanticAddress.ForModule(initial.IdentityCatalog.Application, $"saved-{index}")), SemanticIdentityOrigin.Persisted))],
                [.. entries.Where(entry => entry.Node is EventSyntax).GroupBy(entry => entry.Address!).Select((group, index) => new EventContractIdentityAssignment(group.Key, EventContractId.Create(initial.IdentityCatalog.Application, $"saved-event-{index}"), new(group.Max(entry => ((EventSyntax)entry.Node).Generation)), SemanticIdentityOrigin.Persisted))]);
            var seed = ScreenplayWorkspace.Create(initial.ApplicationName, initial.Documents, catalog);
            if (seed.IdentityCatalog.Semantics.IsEmpty || seed.IdentityCatalog.EventContracts.IsEmpty) _identityFailures.Add($"{sample}: seed has no persisted semantic or event identities");
            var comments = CommentCounts(initial);
            var opened = Result("open-workspace", new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(seed)) });
            foreach (var layout in new[] { "module", "feature", "slice" })
            {
                var context = $"{sample}/{layout}";
                try
                {
                    var proposal = Result("expand-layout", new
                    {
                        expectedRevision = opened.GetProperty("revision").GetString(),
                        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
                        layout,
                        validation = "Authoring",
                        formatting = "CanonicalizeTouchedDocuments"
                    });
                    if (proposal.GetProperty("droppedCommentCount").GetInt32() != 0) _commentFailures.Add($"{context}: proposal dropped comments");
                    var candidate = SampleCandidate(proposal, context);
                    if (candidate is null) break;
                    if (!seed.IdentityCatalog.Semantics.All(assignment => candidate.IdentityCatalog.Semantics.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)) ||
                        !seed.IdentityCatalog.EventContracts.All(assignment => candidate.IdentityCatalog.EventContracts.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)))
                    {
                        _identityFailures.Add($"{context}: persisted semantic or event identities changed");
                    }

                    opened = Apply(opened, proposal).GetProperty("workspace");
                    var currentComments = CommentCounts(Workspace());
                    foreach (var (text, count) in comments)
                    {
                        currentComments.TryGetValue(text, out var currentCount);
                        if (currentCount != count)
                        {
                            var locations = Workspace().Documents.SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens
                                .Where(token => token.Kind == WorkspaceSourceTokenKind.Comment && token.Text == text)
                                .Select(token => $"{document.Path.Value}:{token.Span.Line}"));
                            _commentFailures.Add($"{context}: expected {count}, got {currentCount}: {text}; {string.Join(", ", locations)}");
                        }
                    }

                    var compiled = new PlayFileCompiler().CompileApplication(Path.Combine(output, "application.play"));
                    if (!compiled.Result.Success || compiled.Sources.Count() != Root.Read().Length) _reachabilityFailures.Add($"{context}: root compilation did not reach every document");
                    if (layout == "slice")
                    {
                        var slices = Root.Read().Where(document => document.Text.Split('\n').Any(line => line.TrimStart().StartsWith("slice ", StringComparison.Ordinal))).ToArray();
                        if (slices.Length == 0 || !slices.All(document => document.Text.Split('\n').Count(line => line.TrimStart().StartsWith("slice ", StringComparison.Ordinal)) == 1 &&
                            !document.Text.Split('\n').Any(line => line.TrimStart().StartsWith("module ", StringComparison.Ordinal) || line.TrimStart().StartsWith("feature ", StringComparison.Ordinal))))
                        {
                            _sliceDocumentFailures.Add($"{context}: missing slice documents or slices with scope restatements");
                        }
                    }
                }
                catch (McpFailure failure)
                {
                    _workflowFailures.Add($"{context}: {failure.Message}");
                    break;
                }
            }
        }
    }

    static Dictionary<string, int> CommentCounts(ScreenplayWorkspace workspace) => workspace.Documents
        .SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens)
        .Where(token => token.Kind == WorkspaceSourceTokenKind.Comment)
        .GroupBy(token => token.Text, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    ScreenplayWorkspace? SampleCandidate(JsonElement proposal, string context)
    {
        var bytes = new List<byte>();
        var offset = 0;
        while (true)
        {
            var page = Result("export-workspace", new
            {
                expectedRevision = proposal.GetProperty("after").GetProperty("revision").GetString(),
                proposalId = proposal.GetProperty("proposalId").GetString(),
                offset,
                limit = 192 * 1024
            });
            bytes.AddRange(page.GetProperty("bytesBase64").GetBytesFromBase64());
            if (page.GetProperty("nextOffset").ValueKind == JsonValueKind.Null)
            {
                if (bytes.Count != page.GetProperty("totalBytes").GetInt32())
                {
                    _workflowFailures.Add($"{context}: exported workspace byte count differs from totalBytes");
                    return null;
                }

                return ScreenplayWorkspaceSerializer.Deserialize([.. bytes]);
            }

            offset = page.GetProperty("nextOffset").GetInt32();
        }
    }

    [Fact] void should_admit_and_apply_every_sample_layout() => _workflowFailures.ShouldBeEmpty();
    [Fact] void should_keep_the_same_semantic_and_event_identities() => _identityFailures.ShouldBeEmpty();
    [Fact] void should_keep_each_authored_comment_once_from_either_import_style() => _commentFailures.ShouldBeEmpty();
    [Fact] void should_compile_the_complete_generated_model_from_its_root() => _reachabilityFailures.ShouldBeEmpty();
    [Fact] void should_write_one_slice_per_document_without_scope_restatements() => _sliceDocumentFailures.ShouldBeEmpty();
}
