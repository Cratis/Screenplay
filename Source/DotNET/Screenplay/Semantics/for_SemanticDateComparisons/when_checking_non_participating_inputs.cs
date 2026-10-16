// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_non_participating_inputs : given.a_date_command
{
    SemanticValidationFailure? _result;

    void Because() => _result = SemanticDateComparisons.CheckInputs(Application, Command, [new(Unused.Id, SemanticValue.Text("2026-01-01T00:00"))]);

    [Fact] void should_ignore_an_offsetless_non_participant() => _result.ShouldBeNull();
}
