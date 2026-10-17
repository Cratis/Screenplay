// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_proposing_notes_on_bound_split_containers : given.a_split_notes_workspace
{
    readonly List<(SemanticKind Kind, JsonElement Proposal, JsonElement Note)> _results = [];

    void Establish() => OpenSplit();

    void Because()
    {
        foreach (var kind in new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice })
        {
            foreach (var target in Targets(kind))
            {
                var proposal = ProposeNote(target);
                _results.Add((kind, proposal, Review(proposal)));
            }
        }
    }

    [Fact] void should_bind_the_control() => Split.Compilation.Success.ShouldBeTrue();
    [Fact] void should_accept_every_target_form() => _results.TrueForAll(result => result.Proposal.GetProperty("success").GetBoolean()).ShouldBeTrue();
    [Fact] void should_keep_each_requested_container() => _results.TrueForAll(result => result.Note.GetProperty("lastAddress").GetProperty("kind").GetString() == result.Kind.ToString()).ShouldBeTrue();
    [Fact] void should_resolve_dotted_typed_and_handle_targets_identically() => _results.GroupBy(result => result.Kind).All(group => group.Select(result => result.Note.GetProperty("anchor").GetRawText()).Distinct().Count() == 1).ShouldBeTrue();
    [Fact] void should_not_report_a_substitution() => _results.TrueForAll(result => result.Proposal.GetProperty("substitutions").GetArrayLength() == 0).ShouldBeTrue();
}
