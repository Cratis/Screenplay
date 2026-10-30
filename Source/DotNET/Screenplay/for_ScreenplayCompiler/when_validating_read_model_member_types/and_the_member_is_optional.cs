// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_member_is_optional : given.a_member
{
    protected override string MemberType => "Child optional";
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_reject_an_optional_read_model_reference() => Result.Diagnostics.Single().Code.ShouldEqual("PLAY0273");
    [Fact] void should_name_the_base_type() => Result.Diagnostics.Single().Message.ShouldEqual(Message);
}
