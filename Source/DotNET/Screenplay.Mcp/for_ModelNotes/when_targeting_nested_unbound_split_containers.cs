// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_targeting_nested_unbound_split_containers : given.a_split_notes_workspace
{
    readonly List<(string Expected, JsonElement Proposal, JsonElement Note)> _results = [];

    void Establish()
    {
        OpenSplit(unbound: true);
        File.WriteAllText(Path.Combine(RootPath, "Orders/Orders.play"), "module Orders\n  feature Fulfilment\n    feature Drafts\n      import \"PlaceOrder.play\"\n");
        Split = ScreenplayWorkspace.Create("Acme", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
        Index = WorkspaceSyntaxIndex.Create(Split);
        Opened = Result(Workspaces.Open(Arguments(new { applicationName = "Acme" })));
    }

    void Because()
    {
        foreach (var entry in Index.Entries.Where(entry => entry.Address?.Kind is SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice).GroupBy(entry => entry.Address).Select(group => group.First()))
        {
            JsonElement[] targets =
            [
                Arguments(new { address = string.Join('.', entry.Address!.Parts.Skip(1).Select(part => part.Key)), kind = entry.Address.Kind.ToString() }),
                Arguments(McpSemanticAddresses.Describe(entry.Address)),
                Arguments(McpAstHandles.Describe(entry.Handle))
            ];
            foreach (var target in targets)
            {
                var proposal = ProposeNote(target);
                _results.Add((entry.SemanticId!.Value.ToString(), proposal, Review(proposal)));
            }
        }
    }

    [Fact] void should_exercise_the_module_both_feature_levels_and_slice_in_all_forms() => _results.Count.ShouldEqual(12);
    [Fact] void should_anchor_each_note_to_the_requested_element() => _results.TrueForAll(result => result.Note.GetProperty("anchor").GetProperty("semanticId").GetString() == result.Expected).ShouldBeTrue();
    [Fact] void should_report_no_substitutions() => _results.TrueForAll(result => result.Proposal.GetProperty("substitutions").GetArrayLength() == 0).ShouldBeTrue();
}
