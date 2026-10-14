// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_locating_a_duplicate_member : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => File.WriteAllText(GatePath, "{\n  \"validation\": {},\n  \"validation\": {}\n}");
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_locate_the_second_occurrence_line() => _result.Failure.Line.ShouldEqual(3);
    [Fact] void should_locate_the_second_occurrence_column() => _result.Failure.Column.ShouldEqual(3);
}
