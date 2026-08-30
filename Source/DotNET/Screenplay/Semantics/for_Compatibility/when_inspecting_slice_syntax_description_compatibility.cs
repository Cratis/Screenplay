// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_slice_syntax_description_compatibility : Specification
{
    Type[] _sliceConstructor;

    void Because() => _sliceConstructor = [.. typeof(SliceSyntax).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType)];

    [Fact] void should_keep_the_slice_constructor_unchanged() => _sliceConstructor.ShouldEqual(
        typeof(SliceType),
        typeof(string),
        typeof(IEnumerable<EventSyntax>),
        typeof(IEnumerable<CommandSyntax>),
        typeof(IEnumerable<QuerySyntax>),
        typeof(IEnumerable<ProjectionSyntax>),
        typeof(IEnumerable<CaptureSyntax>),
        typeof(IEnumerable<ReactionSyntax>),
        typeof(IEnumerable<ScreenSyntax>),
        typeof(IEnumerable<ConstraintSyntax>),
        typeof(IEnumerable<SpecificationSyntax>),
        typeof(Diagnostics.SourceLocation),
        typeof(string),
        typeof(IEnumerable<ReadModelSyntax>),
        typeof(IEnumerable<ReducerSyntax>));
    [Fact] void should_add_description_location_as_an_init_property() => typeof(SliceSyntax).GetProperty(nameof(SliceSyntax.DescriptionLocation)).ShouldNotBeNull();
    [Fact] void should_add_description_raw_length_as_an_init_property() => typeof(SliceSyntax).GetProperty(nameof(SliceSyntax.DescriptionRawLength)).ShouldNotBeNull();
}
