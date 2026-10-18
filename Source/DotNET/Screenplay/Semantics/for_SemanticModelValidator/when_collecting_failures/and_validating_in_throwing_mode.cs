// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_collecting_failures;

public class and_validating_in_throwing_mode : given.an_application
{
    Exception _error;

    void Establish() => _application = Application(_first with { Kind = SemanticSliceKind.Unknown }, _second with { Kind = (SemanticSliceKind)999 });
    void Because() => _error = Catch.Exception(() => SemanticModelValidator.Validate(_application));

    [Fact] void should_throw_the_contract_exception() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_stop_at_the_first_error_with_its_original_message() => _error.Message.ShouldEqual("The slice kind value '-1' is unknown.");
}
