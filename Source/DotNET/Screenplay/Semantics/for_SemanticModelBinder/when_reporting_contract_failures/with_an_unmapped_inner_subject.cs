// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_reporting_contract_failures;

public class with_an_unmapped_inner_subject : Specification
{
    readonly SourceLocation _sliceLocation = new(4, 5, "slice.play");
    SourceLocation _location;

    void Because()
    {
        var slice = SemanticId.Create(SemanticKind.Slice, "mapped");
        var specification = SemanticId.Create(SemanticKind.Specification, "unmapped");
        _location = SemanticModelBinder.FailureLocation(
            new([slice, specification], "Invalid specification"),
            new Dictionary<SemanticId, SourceLocation> { [slice] = _sliceLocation },
            SourceLocation.Start);
    }

    [Fact] void should_use_the_nearest_enclosing_declaration() => _location.ShouldEqual(_sliceLocation);
}
