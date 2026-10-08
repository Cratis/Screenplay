// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_automap_covers_the_shape : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
            name String
          readmodel R
            id Uuid
            name String
          projection P => R
            from Changed
              id = $eventSourceId
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_automap_and_event_source_identity() => Findings.ShouldBeEmpty();
}
