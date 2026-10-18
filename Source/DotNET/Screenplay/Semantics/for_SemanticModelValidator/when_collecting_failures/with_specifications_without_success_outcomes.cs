// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_collecting_failures;

public class with_specifications_without_success_outcomes : given.an_application
{
    ImmutableArray<SemanticModelValidator.SemanticValidationFailure> _failures;
    SemanticSpecification _firstSpecification;
    SemanticSpecification _secondSpecification;

    void Establish()
    {
        var command = new SemanticCommand(Id(SemanticKind.Command, "Record"), "Record", [], [], []);
        _firstSpecification = new(Id(SemanticKind.Specification, "First"), "First", [], [], new(command.Id, []), [], [], [], []);
        _secondSpecification = _firstSpecification with { Id = Id(SemanticKind.Specification, "Second"), Name = "Second" };
        _application = Application(_first with { Commands = [command], Specifications = [_firstSpecification, _secondSpecification] });
    }

    void Because() => _failures = SemanticModelValidator.Collect(_application);

    [Fact] void should_report_each_specification_in_the_same_slice() => _failures.Length.ShouldEqual(2);
    [Fact] void should_identify_the_first_specification_inside_its_slice() => _failures[0].Subjects.SequenceEqual([_first.Id, _firstSpecification.Id]).ShouldBeTrue();
    [Fact] void should_identify_the_second_specification_inside_its_slice() => _failures[1].Subjects.SequenceEqual([_first.Id, _secondSpecification.Id]).ShouldBeTrue();
    [Fact] void should_report_the_missing_success_outcome() => _failures[0].Message.ShouldEqual("A success specification must contain at least one success outcome.");
}
