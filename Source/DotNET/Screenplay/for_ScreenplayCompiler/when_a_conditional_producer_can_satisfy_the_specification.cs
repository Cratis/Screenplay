// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_conditional_producer_can_satisfy_the_specification : given.a_compiler
{
    const string Source =
        """
        module Records
          feature Entries
            slice StateChange Record
              command RecordEntry
                note String
                produces when note == "other"
                  Recorded
                    note = "other"
                produces when note == "authored"
                  Recorded
                    note = note
              event Recorded
                note String
              specification Possible
                when RecordEntry
                  note = "authored"
                then Recorded
                  note = "authored"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_require_every_alternative_to_produce_the_expected_value() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_consistency_error() => _result.Diagnostics.ShouldBeEmpty();
}
