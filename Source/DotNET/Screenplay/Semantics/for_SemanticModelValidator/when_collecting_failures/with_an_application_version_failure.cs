// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_collecting_failures;

public class with_an_application_version_failure : given.an_application
{
    ImmutableArray<SemanticModelValidator.SemanticValidationFailure> _failures;

    void Establish() => _application = Application(_first with { Kind = SemanticSliceKind.Unknown }, _second with { Kind = SemanticSliceKind.Unknown });
    void Because() => _failures = SemanticModelValidator.Collect(_application, SemanticVersion.V17);

    [Fact] void should_stop_at_the_application_wide_version_failure() => _failures.Length.ShouldEqual(1);
    [Fact] void should_not_assign_a_declaration_subject() => _failures.Single().Subjects.ShouldBeEmpty();
    [Fact] void should_preserve_the_version_failure_message() => _failures.Single().Message.ShouldEqual("An ESM v17 model must contain an explicit no-event assertion or recovery redelivery.");
}
