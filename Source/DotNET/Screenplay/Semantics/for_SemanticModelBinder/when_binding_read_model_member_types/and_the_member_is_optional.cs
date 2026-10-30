// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types;

public class and_the_member_is_optional : given.a_member
{
    protected override string MemberType => "Child optional";
    void Because() => BindParsedSource();

    [Fact] void should_reject_the_optional_reference_before_binding() => Binding.Diagnostics.Single().Message.ShouldEqual(Message);
}
