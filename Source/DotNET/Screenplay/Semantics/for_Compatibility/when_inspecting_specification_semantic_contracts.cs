// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_specification_semantic_contracts : Specification
{
    NullabilityState _when;
    Type[] _parameters;

    void Because()
    {
        var constructor = typeof(SemanticSpecification).GetConstructors().Single();
        _parameters = [.. constructor.GetParameters().Select(_ => _.ParameterType)];
        _when = new NullabilityInfoContext().Create(constructor.GetParameters()[4]).ReadState;
    }

    [Fact] void should_keep_the_nine_positional_members() => _parameters.Length.ShouldEqual(9);
    [Fact] void should_keep_the_command_binary_type_while_admitting_absence() => _parameters[4].ShouldEqual(typeof(SemanticSpecificationCommand));
    [Fact] void should_mark_the_command_nullable() => _when.ShouldEqual(NullabilityState.Nullable);
}
