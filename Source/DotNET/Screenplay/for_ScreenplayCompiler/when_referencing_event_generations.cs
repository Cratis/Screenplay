// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_referencing_event_generations : given.a_compiler
{
    const string Source =
        """
        module Records
          feature Changes
            slice Translate Import
              event Recorded generation 1
                old String
              event Recorded generation 2
                current String
              command Record
                produces Recorded
                  old = "not current"
                  current = "current"
              reaction React
                when Recorded
                  old
                  current
              capture ImportRecords
                append Recorded
                  when current
                    old = $.old
                    current = $.current
              specification RecordChange
                given Recorded
                  old = "previous"
                  current = "current"
                when Record
                then Recorded
                  old = "previous"
                  current = "current"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_check_produces_and_append_against_the_current_shape() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEventField).ShouldEqual(4);
    [Fact] void should_check_reaction_data_against_the_current_shape() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownTriggerData).ShouldEqual(1);
    [Fact] void should_not_report_a_current_field_as_missing() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("no field 'current'", StringComparison.Ordinal) || diagnostic.Message.Contains("carries no 'current'", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact]
    void should_check_projection_completeness_against_the_current_shape()
    {
        const string source =
            """
            type Row
              old String
              current String
            module Records
              feature Changes
                slice StateView Browse
                  event Recorded generation 1
                    old String
                  event Recorded generation 2
                    current String
                  readmodel View
                    rows Row[]
                  projection Build => View
                    children rows identified by current
                      from Recorded
            """;
        var result = _compiler.Compile(source);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnpopulatedProjectionField).ShouldEqual(1);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnpopulatedProjectionField).Message.ShouldContain("'old'");
    }
}
