// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_collecting_failures;

public class with_a_registration_failure : given.an_application
{
    ImmutableArray<SemanticModelValidator.SemanticValidationFailure> _failures;

    void Establish() => _application = Application(_first with { Kind = SemanticSliceKind.Unknown }, _second with { Id = _first.Id });
    void Because() => _failures = SemanticModelValidator.Collect(_application);

    [Fact] void should_stop_before_reference_validation() => _failures.Length.ShouldEqual(1);
    [Fact] void should_report_the_duplicate_identity() => _failures.Single().Message.ShouldEqual($"Semantic identity '{_first.Id}' is duplicated.");
    [Fact] void should_have_no_declaration_subject() => _failures.Single().Subjects.ShouldBeEmpty();
}
