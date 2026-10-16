// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_absent_inputs : given.a_date_command
{
    SemanticValidationFailure? _result;

    void Because() => _result = SemanticDateComparisons.CheckInputs(Application, Command, [null!, new(First.Id, SemanticValue.Null)]);

    [Fact] void should_ignore_null_entries_absent_values_and_missing_properties() => _result.ShouldBeNull();
}
