// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_type_is_slice_local : given.a_member
{
    protected override string SliceDeclarations => "      type Child\n        value String\n";
    void Because() => BindParsedSource();

    [Fact] void should_not_hoist_the_skipped_type_into_the_value_namespace() => Binding.Diagnostics.Single().Message.ShouldEqual(Message);
}
