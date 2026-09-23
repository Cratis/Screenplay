// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_migrating_a_concept_across_a_strict_document_batch : given.an_authoring_connection
{
    const string Concepts = "concept ProjectId : Uuid\nconcept ProjectName : String\n";
    JsonElement _opened;
    JsonElement _proposal;
    JsonElement _applied;
    JsonElement _originalIdentity;
    JsonElement _migratedIdentity;
    ScreenplayWorkspace _candidate = null!;
    bool _unchanged;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "concepts.play"), Concepts);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source[Source.IndexOf("module Projects", StringComparison.Ordinal)..]);
        Initialize();
    }

    void Because()
    {
        _opened = Open();
        var revision = _opened.GetProperty("revision").GetString();
        var documents = Page("documents", revision);
        _originalIdentity = Page("semantics", revision).EnumerateArray().Single(item => item.GetProperty("address").GetProperty("parts").EnumerateArray().Last().GetProperty("key").GetString() == "ProjectName");
        var currentAddress = JsonNode.Parse(_originalIdentity.GetProperty("address").GetRawText());
        currentAddress["parts"].AsArray()[^1]["key"] = "ProjectTitle";
        _proposal = Result("propose", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            operations = documents.EnumerateArray().Select(document => new
            {
                operation = "replace-document",
                documentId = document.GetProperty("documentId").GetString(),
                bytesBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(RootPath, document.GetProperty("path").GetString())).Replace("ProjectName", "ProjectTitle", StringComparison.Ordinal)))
            }).ToArray(),
            semanticRenames = new[] { new { previousAddress = _originalIdentity.GetProperty("address"), currentAddress } }
        });
        _unchanged = File.ReadAllText(Path.Combine(RootPath, "concepts.play")) == Concepts && File.ReadAllText(Path.Combine(RootPath, "application.play")) == Source[Source.IndexOf("module Projects", StringComparison.Ordinal)..];
        _candidate = Candidate(_proposal);
        _applied = Apply(_opened, _proposal);
        _migratedIdentity = Page("semantics", _applied.GetProperty("workspace").GetProperty("revision").GetString()).EnumerateArray()
            .Single(item => item.GetProperty("address").GetProperty("parts").EnumerateArray().Last().GetProperty("key").GetString() == "ProjectTitle");
    }

    [Fact] void should_open_with_compact_metadata() => _opened.GetProperty("workspaceJson").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_propose_two_coordinated_document_changes() => _proposal.GetProperty("changeCount").GetInt32().ShouldEqual(2);
    [Fact] void should_keep_candidate_metadata_compact() => _proposal.GetProperty("after").GetProperty("workspaceJson").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_leave_both_original_documents_untouched_until_apply() => _unchanged.ShouldBeTrue();
    [Fact] void should_validate_the_complete_candidate_as_executable() => _candidate.Compilation.Success.ShouldBeTrue();
    [Fact] void should_update_every_reference_in_the_candidate() => _candidate.Documents.All(document => document.Text.Contains("ProjectTitle", StringComparison.Ordinal) && !document.Text.Contains("ProjectName", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_apply_the_complete_batch() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_explicitly_migrated_semantic_identity() => _migratedIdentity.GetProperty("semanticId").GetString().ShouldEqual(_originalIdentity.GetProperty("semanticId").GetString());
    [Fact] void should_compile_the_applied_folder() => new McpSnapshot(Root.Read()).Compilation.Success.ShouldBeTrue();
    [Fact] void should_write_the_exact_reviewed_bytes() => Root.Read().All(document => document.Bytes.SequenceEqual(_candidate.Documents.Single(candidate => candidate.Path == document.Path).Bytes)).ShouldBeTrue();
}
