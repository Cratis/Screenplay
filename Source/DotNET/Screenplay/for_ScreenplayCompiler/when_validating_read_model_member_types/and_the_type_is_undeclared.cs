// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types;

public class and_the_type_is_undeclared : given.a_member
{
    protected override string SliceDeclarations => string.Empty;
    void Because() => Result = Compiler.Compile(Source);

    [Fact] void should_compile_successfully() => Result.Success.ShouldBeTrue();
    [Fact] void should_not_report_an_unsupported_member_type() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273").ShouldBeFalse();
}
