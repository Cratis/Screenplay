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
    readonly List<bool> _identities = [];
    readonly List<bool> _comments = [];
    readonly List<bool> _reachable = [];
    readonly List<bool> _sliceDocuments = [];
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
            seed.IdentityCatalog.Semantics.ShouldNotBeEmpty();
            seed.IdentityCatalog.EventContracts.ShouldNotBeEmpty();
            var opened = Result("open-workspace", new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(seed)) });
            foreach (var layout in new[] { "module", "feature", "slice" })
            {
                var proposal = Result("expand-layout", new
                {
                    expectedRevision = opened.GetProperty("revision").GetString(),
                    expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
                    layout,
                    validation = "Authoring",
                    formatting = "CanonicalizeTouchedDocuments"
                });
                _comments.Add(proposal.GetProperty("droppedCommentCount").GetInt32() == 0);
                var candidate = SampleCandidate(proposal);
                _identities.Add(seed.IdentityCatalog.Semantics.All(assignment => candidate.IdentityCatalog.Semantics.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)) &&
                    seed.IdentityCatalog.EventContracts.All(assignment => candidate.IdentityCatalog.EventContracts.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)));
                opened = Apply(opened, proposal).GetProperty("workspace");
                var compiled = new PlayFileCompiler().CompileApplication(Path.Combine(output, "application.play"));
                _reachable.Add(compiled.Result.Success && compiled.Sources.Count() == Root.Read().Length);
                if (layout == "slice")
                {
                    var slices = Root.Read().Where(document => document.Text.Split('\n').Any(line => line.TrimStart().StartsWith("slice ", StringComparison.Ordinal))).ToArray();
                    slices.ShouldNotBeEmpty();
                    _sliceDocuments.Add(slices.All(document => document.Text.Split('\n').Count(line => line.TrimStart().StartsWith("slice ", StringComparison.Ordinal)) == 1 &&
                        !document.Text.Split('\n').Any(line => line.TrimStart().StartsWith("module ", StringComparison.Ordinal) || line.TrimStart().StartsWith("feature ", StringComparison.Ordinal))));
                }
            }
        }
    }

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

    [Fact] void should_admit_every_sample_layout_with_the_same_semantic_and_event_identities() => _identities.ShouldContainOnly(true, true, true, true, true, true);
    [Fact] void should_not_drop_comments_from_either_import_style() => _comments.ShouldContainOnly(true, true, true, true, true, true);
    [Fact] void should_compile_the_complete_generated_model_from_its_root() => _reachable.ShouldContainOnly(true, true, true, true, true, true);
    [Fact] void should_write_one_slice_per_document_without_scope_restatements() => _sliceDocuments.ShouldContainOnly(true, true);
}
