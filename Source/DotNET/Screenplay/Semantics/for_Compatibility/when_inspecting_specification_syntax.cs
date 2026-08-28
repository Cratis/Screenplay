// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_specification_syntax : Specification
{
    Type[] _commandParameterTypes;
    Type[] _eventParameterTypes;
    Type[] _parameterTypes;

    void Because()
    {
        _parameterTypes = [.. typeof(SpecificationSyntax).GetConstructors().Single().GetParameters().Select(_ => _.ParameterType)];
        _eventParameterTypes = [.. typeof(SpecificationEventSyntax).GetConstructors().Single().GetParameters().Select(_ => _.ParameterType)];
        _commandParameterTypes = [.. typeof(SpecificationCommandSyntax).GetConstructors().Single().GetParameters().Select(_ => _.ParameterType)];
    }

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
    [Fact] void should_keep_the_event_constructor_unchanged() => _eventParameterTypes.ShouldEqual(typeof(string), typeof(IEnumerable<PropertyMappingSyntax>), typeof(Diagnostics.SourceLocation));
    [Fact] void should_keep_the_command_constructor_unchanged() => _commandParameterTypes.ShouldEqual(typeof(string), typeof(IEnumerable<PropertyMappingSyntax>), typeof(Diagnostics.SourceLocation));
    [Fact] void should_add_event_source_as_an_event_init_property() => typeof(SpecificationEventSyntax).GetProperty(nameof(SpecificationEventSyntax.For)).ShouldNotBeNull();
    [Fact] void should_add_event_source_as_a_command_init_property() => typeof(SpecificationCommandSyntax).GetProperty(nameof(SpecificationCommandSyntax.For)).ShouldNotBeNull();
}
