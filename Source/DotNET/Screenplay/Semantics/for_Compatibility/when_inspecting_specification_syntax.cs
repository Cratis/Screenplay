// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_specification_syntax : Specification
{
    Type[] _parameterTypes;

    void Because() => _parameterTypes = [.. typeof(SpecificationSyntax).GetConstructors().Single().GetParameters().Select(_ => _.ParameterType)];

    [Fact]
    void should_keep_the_positional_constructor_unchanged() =>
        _parameterTypes.SequenceEqual(
        [
            typeof(string),
            typeof(IEnumerable<SpecificationEventSyntax>),
            typeof(SpecificationCommandSyntax),
            typeof(IEnumerable<SpecificationEventSyntax>),
            typeof(IEnumerable<SpecificationErrorSyntax>),
            typeof(Diagnostics.SourceLocation),
            typeof(IEnumerable<SpecificationReadModelSyntax>),
            typeof(IEnumerable<SpecificationReadModelSyntax>)
        ]).ShouldBeTrue();

    [Fact] void should_add_query_assertions_as_an_init_property() => typeof(SpecificationSyntax).GetProperty(nameof(SpecificationSyntax.ThenQueries)).ShouldNotBeNull();
}
