// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_slice_local_type_is_not_referenced : given.a_member
{
    protected override string MemberType => "String";
    protected override string SliceDeclarations => "      type Child\n        value String\n";
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_remain_warning_only() => Result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_unknown_construct_warning() => Result.Diagnostics.Single().Code.ShouldEqual("PLAY0029");
    [Fact] void should_not_hoist_the_skipped_block() => (Result.Value!.Types ?? []).ShouldBeEmpty();
}
