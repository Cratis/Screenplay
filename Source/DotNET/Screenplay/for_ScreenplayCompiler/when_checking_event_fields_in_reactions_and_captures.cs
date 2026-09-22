// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_event_fields_in_reactions_and_captures : given.a_compiler
{
    const string Source =
        """
        module Records
          feature Changes
            slice Translate Import
              event Recorded
                note String
              reaction React
                when Recorded
                  produces Recorded
                    absent = "reaction"
              capture ImportRecords
                append Recorded
                  when note
                    absent = $.note
                children rows identified by id
                  append Recorded
                    when added
                      absent = $.note
                nested detail
                  append Recorded
                    when note
                      absent = $.note
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_check_every_event_producer_mapping() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventField, DiagnosticCodes.UnknownEventField, DiagnosticCodes.UnknownEventField, DiagnosticCodes.UnknownEventField);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
