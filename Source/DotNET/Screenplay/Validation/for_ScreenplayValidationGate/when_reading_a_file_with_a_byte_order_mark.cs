// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_a_file_with_a_byte_order_mark : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => File.WriteAllBytes(GatePath, [.. System.Text.Encoding.UTF8.GetPreamble(), .. System.Text.Encoding.UTF8.GetBytes("{\"validation\":{\"scope\":\"Billing\"}}")]);
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_not_refuse_the_file() => _result.Failure.ShouldBeNull();
    [Fact] void should_read_the_declared_scope() => _result.Declaration.Scope.ShouldEqual("Billing");
}
