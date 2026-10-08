// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_children_and_nested_blocks_populate_targets : given.a_view
{
    void Establish() => Compile(
        """
        slice StateView View
          event Changed
            value String
          readmodel R
            items Item[]
            detail Item optional
          projection P => R
            children items identified by value
              from Changed
            nested detail
              from Changed
        """,
        "type Item\n  value String\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_the_top_level_container_origins() => Findings.ShouldBeEmpty();
}
