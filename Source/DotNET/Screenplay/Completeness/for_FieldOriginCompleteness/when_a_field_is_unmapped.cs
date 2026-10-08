// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_a_field_is_unmapped : given.a_view
{
    void Establish() => Compile("""
        slice StateView View
          event Changed
            name String
          readmodel R
            name String
            note String optional
          projection P => R
            from Changed
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_report_only_the_unmapped_optional_field() => Findings.Select(finding => finding.Code).ShouldContainOnly(DiagnosticCodes.ReadModelFieldWithoutOrigin);
    [Fact] void should_locate_the_field() => Findings.Single().Location.Line.ShouldEqual(8);
    [Fact] void should_warn_without_failing_compilation() => Findings.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
