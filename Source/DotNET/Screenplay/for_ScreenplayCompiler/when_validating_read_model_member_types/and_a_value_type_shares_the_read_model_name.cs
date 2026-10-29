// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_a_value_type_shares_the_read_model_name : given.a_member
{
    protected override string Declarations => "type Child\n  value String\n";
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_resolve_the_top_level_value_type() => Result.Diagnostics.ShouldBeEmpty();
}
