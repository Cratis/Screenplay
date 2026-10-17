// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_duplicating_an_identified_container : given.a_split_notes_workspace
{
    JsonElement _items;
    readonly List<Exception> _refusals = [];
    byte[]? _notes;
    string _catalog = null!;

    void Establish()
    {
        OpenSplit();
        Apply(ProposeNotes([.. new[] { SemanticKind.Slice, SemanticKind.Command }.Select(kind => (object)new { operation = "add", kind = "note", text = kind.ToString(), author = "team", target = Targets(kind)[0] })]));
        _notes = new McpManagedFiles(Root).Read(ModelNotesStore.FileName);
        _catalog = Opened.GetProperty("catalogRevision").GetString()!;
        var path = Path.Combine(RootPath, "Orders/PlaceOrder.play");
        File.AppendAllText(path, "\n" + File.ReadAllText(path));
        Opened = Result(Workspaces.Open(McpJson.Empty));
        Split = McpState.Deserialize(new McpManagedFiles(Root).Read(McpState.FileName)!).Open(Root);
        Index = WorkspaceSyntaxIndex.Create(Split);
    }

    void Because()
    {
        _items = Result(Workspaces.ListNotes(McpJson.Empty)).GetProperty("page").GetProperty("items");
        foreach (var kind in new[] { SemanticKind.Slice, SemanticKind.Command })
        {
            var assignment = Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == kind);
            foreach (var target in Targets(kind).Append(Arguments(new { semanticId = assignment.Id.ToString() })))
            {
                _refusals.Add(Catch.Exception(() => ProposeNote(target)));
            }
        }
    }

    [Fact] void should_keep_the_catalog() => Opened.GetProperty("catalogRevision").GetString().ShouldEqual(_catalog);
    [Fact] void should_report_assigned_ambiguity() => Opened.GetProperty("containerIdentities").GetProperty("containers")[0].GetProperty("assigned").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_both_notes_resolved() => _items.EnumerateArray().All(item => item.GetProperty("state").GetString() == "open").ShouldBeTrue();
    [Fact] void should_flag_notes_and_descendants_as_ambiguous() => _items.EnumerateArray().All(item => item.GetProperty("ambiguous").GetBoolean()).ShouldBeTrue();
    [Fact] void should_preserve_sidecar_bytes() => McpManagedFiles.Equal(new McpManagedFiles(Root).Read(ModelNotesStore.FileName), _notes).ShouldBeTrue();
    [Fact] void should_check_all_eight_targets() => _refusals.Count.ShouldEqual(8);
    [Fact] void should_refuse_every_target_form_and_descendant() => _refusals.TrueForAll(failure => failure is McpFailure && failure.Message.Contains("ambiguous", StringComparison.Ordinal)).ShouldBeTrue();
}
