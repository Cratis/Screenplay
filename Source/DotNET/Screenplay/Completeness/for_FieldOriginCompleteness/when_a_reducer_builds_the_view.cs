// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_reducer_builds_the_view : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
          readmodel R
            name String
          reducer P => R
            on Changed
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_not_guess_at_opaque_reduction() => Findings.ShouldBeEmpty();
}
