// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_type_is_undeclared : given.a_member
{
    protected override string SliceDeclarations => string.Empty;
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_fail_instead_of_leaving_an_unresolved_member() => Result.Success.ShouldBeFalse();
    [Fact] void should_explain_the_unsupported_type_once() => Result.Diagnostics.Single().Message.ShouldEqual(Message);
}
