// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_refusing_a_file_with_a_byte_order_mark : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => File.WriteAllBytes(GatePath, [.. System.Text.Encoding.UTF8.GetPreamble(), .. System.Text.Encoding.UTF8.GetBytes("{\"unknown\":true}")]);
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_refuse_the_unknown_member() => _result.Failure.Reason.ShouldEqual("Unknown top-level member.");
    [Fact] void should_locate_the_member_without_counting_the_mark() => _result.Failure.Column.ShouldEqual(2);
}
