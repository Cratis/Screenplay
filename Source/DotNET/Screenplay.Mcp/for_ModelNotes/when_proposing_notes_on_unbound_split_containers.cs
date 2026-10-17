// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_proposing_notes_on_unbound_split_containers : given.a_split_notes_workspace
{
    readonly List<(JsonElement Target, JsonElement Proposal, JsonElement Note)> _results = [];

    void Establish() => OpenSplit(unbound: true);

    void Because()
    {
        foreach (var kind in new[] { SemanticKind.Module, SemanticKind.Feature, SemanticKind.Slice })
        {
            foreach (var target in Targets(kind, typed: false))
            {
                var proposal = ProposeNote(target);
                _results.Add((target, proposal, Review(proposal)));
            }
        }
    }

    [Fact] void should_keep_the_model_unbound() => Split.Compilation.Success.ShouldBeFalse();
    [Fact] void should_accept_the_authoring_source() => Opened.GetProperty("sourceSuccess").GetBoolean().ShouldBeTrue();
    [Fact] void should_accept_dotted_and_handle_targets_without_false_ambiguity() => _results.TrueForAll(result => result.Proposal.GetProperty("success").GetBoolean()).ShouldBeTrue();
    [Fact] void should_preserve_the_existing_application_fallback() => _results.TrueForAll(result => result.Note.GetProperty("anchor").GetProperty("application").GetBoolean()).ShouldBeTrue();
    [Fact] void should_report_every_substitution() => _results.TrueForAll(result => result.Proposal.GetProperty("substitutions").GetArrayLength() == 1).ShouldBeTrue();
    [Fact] void should_report_each_requested_target() => _results.TrueForAll(result => result.Proposal.GetProperty("substitutions")[0].GetProperty("target").GetRawText() == result.Target.GetRawText()).ShouldBeTrue();
    [Fact] void should_report_the_actual_resolved_anchor() => _results.TrueForAll(result => result.Proposal.GetProperty("substitutions")[0].GetProperty("anchor").GetRawText() == result.Note.GetProperty("anchor").GetRawText()).ShouldBeTrue();
    [Fact] void should_report_the_application_address() => _results.TrueForAll(result => result.Proposal.GetProperty("substitutions")[0].GetProperty("address").GetProperty("kind").GetString() == "Application").ShouldBeTrue();
    [Fact] void should_still_refuse_an_unidentified_typed_slice() => Catch.Exception(() => ProposeNote(Targets(SemanticKind.Slice)[2])).ShouldBeOfExactType<McpFailure>();
}
