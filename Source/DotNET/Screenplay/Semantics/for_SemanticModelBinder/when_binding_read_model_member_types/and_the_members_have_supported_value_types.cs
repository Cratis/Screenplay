// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_members_have_supported_value_types : given.a_member
{
    protected override string SliceDeclarations => "        parentId String key\n        scalar Child\n        optional Child optional\n        name Name\n        count Int\n";
    protected override string Declarations => "type Child\n  value String\nconcept Name : String\n";
    void Because() => BindParsedSource();

    [Fact] void should_bind_composite_collections_and_scalars() => Binding.Success.ShouldBeTrue();
    [Fact] void should_keep_supported_primitive_and_concept_members() => Binding.Diagnostics.ShouldBeEmpty();
}
