// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_projection_is_realized_by_a_file : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
          readmodel R
            name String
          projection P => R
            file Projections/P.cs
            from Changed
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_not_guess_at_opaque_projection_code() => Findings.ShouldBeEmpty();
}
