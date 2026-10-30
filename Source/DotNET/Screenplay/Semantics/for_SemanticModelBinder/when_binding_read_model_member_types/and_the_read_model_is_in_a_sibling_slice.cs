// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_read_model_is_in_a_sibling_slice : given.a_member
{
    protected override string SliceDeclarations => string.Empty;
    protected override string Siblings => "    slice StateView Other\n      readmodel Child\n        value String\n";
    void Because() => BindParsedSource();

    [Fact] void should_not_expand_read_model_identities_into_value_types() => Binding.Diagnostics.Single().Code.ShouldEqual("PLAY0273");
}
