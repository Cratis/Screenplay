// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_explicit_offset_inputs : given.a_date_command
{
    SemanticValidationFailure? _result;

    void Because() => _result = SemanticDateComparisons.CheckInputs(Application, Command,
        [new(First.Id, SemanticValue.Text("2026-01-01T02:00+02:00")), new(Second.Id, SemanticValue.Text("2026-01-02T00:00Z"))]);

    [Fact] void should_accept_offsets_and_z() => _result.ShouldBeNull();
}
