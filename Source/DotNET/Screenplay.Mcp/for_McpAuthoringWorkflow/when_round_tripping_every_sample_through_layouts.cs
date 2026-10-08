// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_round_tripping_every_sample_through_layouts : given.an_authoring_connection
{
    readonly List<string> _workflowFailures = [];
    readonly List<string> _orderFailures = [];
    readonly List<string> _modelFailures = [];
    readonly List<string> _identityFailures = [];
    readonly List<string> _singleFailures = [];
    readonly List<string> _invoicingCommentFailures = [];
    string _samples = null!;

    void Establish()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, "Samples"))) repository = repository.Parent;
        _samples = Path.Combine(repository!.FullName, "Samples");
    }

    void Because()
    {
        foreach (var sample in new[] { "Commerce", "TimeTracking", "Invoicing", "Library" })
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
            var order = given.a_layout_order.Sequences(seed);
            var comments = CommentCounts(seed);
            var opened = Result("open-workspace", new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(seed)) });
            string? firstSingle = null;
            foreach (var layout in new[] { "single", "module", "feature", "slice", "single" })
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
                    var candidate = SampleCandidate(proposal);
                    if (!order.SequenceEqual(given.a_layout_order.Sequences(candidate))) _orderFailures.Add(context);
                    if (seed.Compilation.Value is not null ? !WorkspaceRepairVerification.SameModel(seed, candidate) : !WorkspaceReferenceLayout.Equivalent(seed, candidate))
                    {
                        _modelFailures.Add(context);
                    }
                    if (!seed.IdentityCatalog.Semantics.All(assignment => candidate.IdentityCatalog.Semantics.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)) ||
                        !seed.IdentityCatalog.EventContracts.All(assignment => candidate.IdentityCatalog.EventContracts.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id && current.Revision == assignment.Revision)))
                    {
                        _identityFailures.Add(context);
                    }
                    if (sample == "Invoicing")
                    {
                        var currentComments = CommentCounts(candidate);
                        if (comments.Count != currentComments.Count || comments.Any(comment => currentComments.GetValueOrDefault(comment.Key) != comment.Value))
                        {
                            _invoicingCommentFailures.Add(context);
                        }
                    }

                    if (layout == "single")
                    {
                        var text = candidate.Documents.Single().Text;
                        if (firstSingle is not null && firstSingle != text)
                        {
                            var line = firstSingle.Split('\n').Zip(text.Split('\n')).Select((pair, index) => (pair.First, pair.Second, Line: index + 1)).FirstOrDefault(pair => pair.First != pair.Second);
                            _singleFailures.Add($"{context}: first differing line {line.Line}; first: {line.First}; final: {line.Second}; lengths {firstSingle.Length}/{text.Length}");
                        }
                        firstSingle ??= text;
                    }

                    opened = Apply(opened, proposal).GetProperty("workspace");
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

    ScreenplayWorkspace SampleCandidate(JsonElement proposal)
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
                bytes.Count.ShouldEqual(page.GetProperty("totalBytes").GetInt32());
                return ScreenplayWorkspaceSerializer.Deserialize([.. bytes]);
            }

            offset = page.GetProperty("nextOffset").GetInt32();
        }
    }

    [Fact] void should_admit_and_apply_every_sample_round_trip() => _workflowFailures.ShouldBeEmpty();
    [Fact] void should_keep_each_scopes_authored_sibling_sequences() => _orderFailures.ShouldBeEmpty();
    [Fact] void should_keep_executable_bytes_or_unbound_syntax_modulo_layout() => _modelFailures.ShouldBeEmpty();
    [Fact] void should_keep_persisted_semantic_and_event_contract_identities() => _identityFailures.ShouldBeEmpty();
    [Fact] void should_finish_every_sample_with_the_exact_first_single_document() => Assert.True(_singleFailures.Count == 0, string.Join('\n', _singleFailures));
    [Fact] void should_preserve_every_invoicing_comment_occurrence_exactly_once() => _invoicingCommentFailures.ShouldBeEmpty();
}
