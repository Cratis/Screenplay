// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_proposing_notes_on_repeated_split_headers : given.a_repeated_split_notes_workspace
{
    readonly List<JsonElement> _proposals = [];

    void Because()
    {
        foreach (var kind in new[] { SemanticKind.Module, SemanticKind.Feature })
        {
            _proposals.Add(ProposeNote(Targets(kind, typed: false)[0]));
        }
    }

    [Fact] void should_accept_the_authoring_source() => Opened.GetProperty("sourceSuccess").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_model_unbound() => Split.Compilation.Success.ShouldBeFalse();
    [Fact] void should_have_two_explicit_module_headers() => Index.Entries.Count(entry => entry.Node is ModuleSyntax { IsPlacement: false }).ShouldEqual(2);
    [Fact] void should_have_two_explicit_feature_headers() => Index.Entries.Count(entry => entry.Node is FeatureSyntax { IsPlacement: false }).ShouldEqual(2);
    [Fact] void should_resolve_both_dotted_container_addresses() => _proposals.TrueForAll(proposal => proposal.GetProperty("success").GetBoolean()).ShouldBeTrue();

    // Decision 0093 items 1, 3 and 8: split headers share container identities before binding, without substitution.
    [Fact] void should_not_substitute_either_logical_container() => _proposals.TrueForAll(proposal => proposal.GetProperty("substitutions").GetArrayLength() == 0).ShouldBeTrue();
}
