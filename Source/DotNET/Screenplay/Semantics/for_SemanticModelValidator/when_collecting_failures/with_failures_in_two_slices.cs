// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_collecting_failures;

public class with_failures_in_two_slices : given.an_application
{
    ImmutableArray<SemanticModelValidator.SemanticValidationFailure> _failures;

    void Establish() => _application = Application(_first with { Kind = SemanticSliceKind.Unknown }, _second with { Kind = SemanticSliceKind.Unknown });
    void Because() => _failures = SemanticModelValidator.Collect(_application);

    [Fact] void should_report_both_failures() => _failures.Length.ShouldEqual(2);
    [Fact] void should_identify_the_first_slice() => _failures[0].Subjects.SequenceEqual([_first.Id]).ShouldBeTrue();
    [Fact] void should_identify_the_second_slice_without_the_first() => _failures[1].Subjects.SequenceEqual([_second.Id]).ShouldBeTrue();
    [Fact] void should_preserve_the_contract_message() => _failures[0].Message.ShouldEqual("The slice kind value '-1' is unknown.");
}
