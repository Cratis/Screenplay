// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_source_map_role_compatibility : Specification
{
    Type[] _entryConstructor;

    void Because() => _entryConstructor = [.. typeof(SemanticSourceMapEntry).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType)];

    [Fact] void should_keep_the_source_map_entry_constructor_unchanged() => _entryConstructor.ShouldEqual(typeof(SemanticId), typeof(SemanticSourceSpan), typeof(SemanticIdentityOrigin));
    [Fact] void should_add_role_as_an_init_property() => typeof(SemanticSourceMapEntry).GetProperty(nameof(SemanticSourceMapEntry.Role)).ShouldNotBeNull();
    [Fact] void should_default_role_to_declaration() => new SemanticSourceMapEntry(default, default, default).Role.ShouldEqual(SemanticSourceMapRole.Declaration);
    [Fact] void should_number_declaration_as_zero() => ((int)SemanticSourceMapRole.Declaration).ShouldEqual(0);
    [Fact] void should_number_description_as_one() => ((int)SemanticSourceMapRole.Description).ShouldEqual(1);
    [Fact] void should_define_unknown_role_as_minus_one() => ((int)SemanticSourceMapRole.Unknown).ShouldEqual(-1);
}
