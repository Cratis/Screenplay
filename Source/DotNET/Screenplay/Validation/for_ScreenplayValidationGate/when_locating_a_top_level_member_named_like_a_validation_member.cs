// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_locating_a_top_level_member_named_like_a_validation_member : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => File.WriteAllText(GatePath, "{\n  \"validation\": { \"scope\": \"Billing\" },\n  \"scope\": true\n}");
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_refuse_the_unknown_top_level_member() => _result.Failure.Reason.ShouldEqual("Unknown top-level member.");
    [Fact] void should_locate_the_top_level_member_line() => _result.Failure.Line.ShouldEqual(3);
    [Fact] void should_locate_the_top_level_member_column() => _result.Failure.Column.ShouldEqual(3);
}
