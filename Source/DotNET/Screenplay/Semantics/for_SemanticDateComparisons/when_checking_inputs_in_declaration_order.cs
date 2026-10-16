// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_inputs_in_declaration_order : given.a_date_command
{
    SemanticValidationFailure? _result;

    void Because() => _result = SemanticDateComparisons.CheckInputs(Application, Command,
        [null!, new(Second.Id, SemanticValue.Text("2026-01-02T00:00")), new(First.Id, SemanticValue.Text("2026-01-01T00:00"))]);

    [Fact] void should_refuse_the_first_declared_property_not_the_first_supplied_value() => _result!.Message.ShouldEqual("DateTime command property 'from' requires an explicit offset or Z.");
}
