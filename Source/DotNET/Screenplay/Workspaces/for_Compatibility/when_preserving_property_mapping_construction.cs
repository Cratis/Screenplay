// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_Compatibility;

public class when_preserving_property_mapping_construction : Specification
{
    PropertyMappingSyntax _mapping = null!;
    string _property = null!;
    ExpressionSyntax _source = null!;
    SourceLocation _location = null!;

    void Because()
    {
        _mapping = new PropertyMappingSyntax("name", new PathExpressionSyntax("name", new(1, 1)), new(1, 1));
        (_property, _source, _location) = _mapping;
    }

    [Fact] void should_keep_the_three_argument_constructor() => typeof(PropertyMappingSyntax).GetConstructor([typeof(string), typeof(ExpressionSyntax), typeof(SourceLocation)]).ShouldNotBeNull();
    [Fact] void should_keep_property_deconstruction() => _property.ShouldEqual("name");
    [Fact] void should_keep_source_deconstruction() => _source.ShouldEqual(_mapping.Source);
    [Fact] void should_keep_location_deconstruction() => _location.ShouldEqual(_mapping.Location);
    [Fact] void should_leave_source_evidence_absent_for_manual_nodes() => _mapping.SourceLocation.ShouldBeNull();
    [Fact] void should_leave_source_length_absent_for_manual_nodes() => _mapping.SourceLength.ShouldBeNull();
}
#endif
