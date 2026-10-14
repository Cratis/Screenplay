// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_locating_a_validation_member_named_like_a_top_level_member : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => File.WriteAllText(GatePath, "{\n  \"$schema\": \"a\",\n  \"validation\": {\n    \"$schema\": \"b\"\n  }\n}");
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_refuse_the_unknown_validation_member() => _result.Failure.Reason.ShouldEqual("Unknown validation member.");
    [Fact] void should_locate_the_member_inside_the_validation_object_line() => _result.Failure.Line.ShouldEqual(4);
    [Fact] void should_locate_the_member_inside_the_validation_object_column() => _result.Failure.Column.ShouldEqual(5);
}
