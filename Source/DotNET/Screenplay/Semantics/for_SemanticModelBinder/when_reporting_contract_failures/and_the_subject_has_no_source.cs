// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_reporting_contract_failures;

public class and_the_subject_has_no_source : Specification
{
    readonly SourceLocation _applicationLocation = new(2, 3, "application.play");
    SourceLocation _location;

    void Because() => _location = SemanticModelBinder.FailureLocation(
        new([SemanticId.Create(SemanticKind.Command, "unmapped")], "Invalid command"), new Dictionary<SemanticId, SourceLocation>(), _applicationLocation);

    [Fact] void should_fall_back_to_the_application_location() => _location.ShouldEqual(_applicationLocation);
}
