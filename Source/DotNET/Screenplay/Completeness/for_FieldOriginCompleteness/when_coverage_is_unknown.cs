// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_coverage_is_unknown : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          readmodel R
            name String
          projection P => R
            all
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_not_report_an_unknown_automap_source() => Findings.ShouldBeEmpty();
}
