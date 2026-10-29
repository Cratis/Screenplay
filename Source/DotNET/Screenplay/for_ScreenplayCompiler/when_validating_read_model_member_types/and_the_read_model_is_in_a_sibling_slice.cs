// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_read_model_is_in_a_sibling_slice : given.a_member
{
    protected override string SliceDeclarations => string.Empty;
    protected override string Siblings => "    slice StateView Other\n      readmodel Child\n        value String\n";
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_not_register_read_models_as_global_value_types() => Result.Diagnostics.Single().Code.ShouldEqual("PLAY0273");
}
