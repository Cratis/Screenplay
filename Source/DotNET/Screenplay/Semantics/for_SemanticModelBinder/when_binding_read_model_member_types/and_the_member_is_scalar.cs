// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_member_is_scalar : given.a_member
{
    protected override string MemberType => "Child";
    void Because() => BindParsedSource();

    [Fact] void should_reject_the_scalar_reference_before_binding() => Binding.Diagnostics.Single().Message.ShouldEqual(Message);
}
