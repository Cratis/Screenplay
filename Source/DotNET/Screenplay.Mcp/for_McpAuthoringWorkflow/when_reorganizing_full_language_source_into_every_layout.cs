// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reorganizing_full_language_source_into_every_layout : given.an_authoring_connection
{
    readonly Dictionary<string, bool> _equivalent = new(StringComparer.Ordinal);
    readonly List<bool> _pureProposals = [];
    readonly List<bool> _preservedIdentities = [];
    readonly List<bool> _sourceOnly = [];
    readonly List<bool> _canonicalized = [];
    readonly Dictionary<string, int> _documentCounts = new(StringComparer.Ordinal);
    ScreenplayWorkspace _seed = null!;
    JsonElement _opened;

    void Establish()
    {
        _seed = Workspace();
        _seed.IdentityCatalog.Semantics.ShouldNotBeEmpty();
        _seed.IdentityCatalog.EventContracts.ShouldNotBeEmpty();
        var original = _seed.Documents.Single();
        var source = WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, Encoding.UTF8.GetBytes(FullSource));
        var workspace = ScreenplayWorkspace.Create("Projects", [source], _seed.IdentityCatalog);
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), [.. source.Bytes]);
        Initialize();
        _opened = Result("open-workspace", new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(workspace)) });
    }

    void Because()
    {
        var expected = Normalize(new ScreenplayCompiler().Compile(FullSource).Value);
        foreach (var layout in new[] { "single", "module", "feature", "slice" })
        {
            var original = Root.Read();
            var proposal = Result("expand-layout", new
            {
                expectedRevision = _opened.GetProperty("revision").GetString(),
                expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                layout,
                validation = "Authoring",
                formatting = "CanonicalizeTouchedDocuments"
            });
            var unchanged = Root.Read();
            _pureProposals.Add(unchanged.Length == original.Length && unchanged.All(document => document.Bytes.SequenceEqual(original.Single(before => before.Path == document.Path).Bytes)));
            _sourceOnly.Add(proposal.GetProperty("validation").GetString() == "Authoring" && !proposal.GetProperty("after").GetProperty("executableReady").GetBoolean());
            _canonicalized.Add(proposal.GetProperty("canonicalizedSource").GetBoolean());
            var candidate = Candidate(proposal);
            _preservedIdentities.Add(_seed.IdentityCatalog.Semantics.All(assignment => candidate.IdentityCatalog.Semantics.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)) &&
                _seed.IdentityCatalog.EventContracts.All(assignment => candidate.IdentityCatalog.EventContracts.Any(current => current.Address.Equals(assignment.Address) && current.Id == assignment.Id)));
            _opened = Apply(_opened, proposal).GetProperty("workspace");
            var compilation = new McpSnapshot(Root.Read()).Compilation;
            compilation.Success.ShouldBeTrue();
            _equivalent.Add(layout, JsonNode.DeepEquals(expected, Normalize(compilation.Value)));
            _documentCounts.Add(layout, Root.Read().Length);
        }
    }

    [Fact] void should_preserve_the_complete_single_document_model() => _equivalent["single"].ShouldBeTrue();
    [Fact] void should_preserve_the_complete_module_model() => _equivalent["module"].ShouldBeTrue();
    [Fact] void should_preserve_the_complete_feature_model() => _equivalent["feature"].ShouldBeTrue();
    [Fact] void should_preserve_the_complete_slice_model() => _equivalent["slice"].ShouldBeTrue();
    [Fact] void should_propose_all_four_layouts_without_writes() => _pureProposals.ShouldContainOnly(true, true, true, true);
    [Fact] void should_keep_supported_semantic_and_event_identities_in_every_layout() => _preservedIdentities.ShouldContainOnly(true, true, true, true);
    [Fact] void should_never_misrepresent_full_source_as_executable() => _sourceOnly.ShouldContainOnly(true, true, true, true);
    [Fact] void should_disclose_canonicalization_for_every_layout() => _canonicalized.ShouldContainOnly(true, true, true, true);
    [Fact] void should_write_one_single_document() => _documentCounts["single"].ShouldEqual(1);
    [Fact] void should_write_root_and_two_module_documents() => _documentCounts["module"].ShouldEqual(3);
    [Fact] void should_split_more_finely_by_feature_than_module() => _documentCounts["feature"].ShouldBeGreaterThan(_documentCounts["module"]);
    [Fact] void should_split_more_finely_by_slice_than_feature() => _documentCounts["slice"].ShouldBeGreaterThan(_documentCounts["feature"]);

    static JsonNode Normalize(ApplicationSyntax application)
    {
        var node = JsonNode.Parse(SyntaxJson.Serialize(application).GetRawText());
        SortHierarchy(node);
        return node;
    }

    static void SortHierarchy(JsonNode node)
    {
        if (node is JsonObject value)
        {
            foreach (var property in value.ToArray().Where(property => property.Value is not null))
            {
                SortHierarchy(property.Value);
                if ((property.Key == "modules" || property.Key == "features" || property.Key == "slices") && property.Value is JsonArray children)
                {
                    value[property.Key] = new JsonArray([.. children.OrderBy(child => child["name"].GetValue<string>(), StringComparer.Ordinal).Select(child => child.DeepClone())]);
                }
            }
        }
        else if (node is JsonArray values)
        {
            foreach (var child in values.Where(child => child is not null))
            {
                SortHierarchy(child);
            }
        }
    }
}
