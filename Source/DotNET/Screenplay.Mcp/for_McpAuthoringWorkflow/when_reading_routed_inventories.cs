// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_routed_inventories : given.an_authoring_connection
{
    [Fact]
    void should_report_source_and_owned_stream_semantic_ids_in_the_original_ast()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_renaming_a_source_with_streams.RoutedSource);
        Initialize();
        var opened = Open();
        foreach (var (kind, semanticKind) in new[] { ("EventSourceSyntax", SemanticKind.EventSource), ("EventStreamSyntax", SemanticKind.EventStream) })
        {
            var result = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind, limit = 20 });
            var ids = result.GetProperty("page").GetProperty("items").EnumerateArray().Select(entry => entry.GetProperty("semanticId").GetString()).ToArray();
            ids.All(id => id is not null).ShouldBeTrue();
            ids.ShouldContainOnly(Workspace().IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == semanticKind).Select(assignment => assignment.Id.ToString()));
        }
    }

    [Fact]
    void should_keep_the_source_free_catalog_golden_bytes()
    {
        var catalog = canonical_serialization_golden_vectors.CreateIdentityCatalog();
        catalog.Semantics.Any(assignment => assignment.Address.Kind is SemanticKind.EventSource or SemanticKind.EventStream).ShouldBeFalse();
        SemanticIdentityCatalogSerializer.Serialize(catalog).ShouldEqual(canonical_serialization_golden_vectors.IdentityCatalogBytes);
    }

    [Fact]
    void should_separate_construct_readiness_from_whole_workspace_binding_and_catalog_identity()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_renaming_a_source_with_streams.RoutedSource);
        Initialize();
        var opened = Open();
        foreach (var view in new[] { "event-sources", "event-streams", "command-routes" })
        {
            var result = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view, limit = 20 });
            result.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
            result.GetProperty("executionAvailable").GetBoolean().ShouldBeTrue();
            result.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
            foreach (var entry in result.GetProperty("page").GetProperty("items").EnumerateArray())
            {
                entry.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
                entry.GetProperty("executionAvailable").GetBoolean().ShouldBeTrue();
                if (view != "command-routes") entry.GetProperty("semanticId").GetString().ShouldNotBeNull();
            }
        }
    }
}
