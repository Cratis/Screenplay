// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_type_is_undeclared : given.a_member
{
    protected override string SliceDeclarations => string.Empty;
    void Because() => BindParsedSource();

    [Fact] void should_fail_before_the_general_binding_exception_fallback() => Binding.Diagnostics.Single().Message.ShouldEqual(Message);
}
