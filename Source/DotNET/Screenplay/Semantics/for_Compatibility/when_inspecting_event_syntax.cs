// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_event_syntax : Specification
{
    Type[] _parameterTypes;

    void Because() => _parameterTypes = [.. typeof(EventSyntax).GetConstructors().Single().GetParameters().Select(_ => _.ParameterType)];

    [Fact] void should_keep_the_positional_constructor_unchanged() => _parameterTypes.ShouldContainOnly(typeof(string), typeof(IEnumerable<PropertySyntax>), typeof(Diagnostics.SourceLocation), typeof(IEnumerable<TagSyntax>));
    [Fact] void should_not_add_event_contract_grammar_yet() => typeof(EventSyntax).GetProperty("Contract").ShouldBeNull();
}
