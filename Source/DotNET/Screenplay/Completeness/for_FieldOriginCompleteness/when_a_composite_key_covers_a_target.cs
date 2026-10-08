// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_composite_key_covers_a_target : given.a_view
{
    void Establish() => Compile(
        """
        slice StateView View
          event Changed
            sourceId Uuid
          readmodel R
            id Uuid
          projection P => R
            no automap
            from Changed
              key Identity
                id = sourceId
        """,
        "type Identity\n  id Uuid\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_the_explicit_key_target() => Findings.ShouldBeEmpty();
}
