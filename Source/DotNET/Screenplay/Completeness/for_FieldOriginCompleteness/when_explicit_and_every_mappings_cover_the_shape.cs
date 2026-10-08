// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_explicit_and_every_mappings_cover_the_shape : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
            value String
          readmodel R
            name String
            count Int
            occurred DateTime
          projection P => R
            no automap
            every
              occurred = $eventContext.occurred
            from Changed
              name = value
              count count
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_explicit_context_and_count_origins() => Findings.ShouldBeEmpty();
}
