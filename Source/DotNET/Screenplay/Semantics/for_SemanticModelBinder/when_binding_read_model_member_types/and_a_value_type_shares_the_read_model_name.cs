// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_a_value_type_shares_the_read_model_name : given.a_member
{
    protected override string SliceDeclarations => "        parentId String key\n      readmodel Child\n        childId String key\n        value String\n";
    protected override string Declarations => "type Child\n  value String\n";
    void Because() => BindParsedSource();

    [Fact] void should_bind_the_value_type_without_changing_read_model_identity() => Binding.Success.ShouldBeTrue();
}
