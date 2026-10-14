// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_a_missing_file : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_report_absence_without_a_failure() => _result.Failure.ShouldBeNull();
    [Fact] void should_not_invent_a_declaration() => _result.Declaration.ShouldBeNull();
    [Fact] void should_still_report_the_expected_path() => _result.GateFilePath.ShouldEqual(GatePath);
}
