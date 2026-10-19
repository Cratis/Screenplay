// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_anonymous_policies;

public class and_anonymous_is_combined_with_another_condition : given.a_compiler
{
    [Theory]
    [InlineData("anonymous or authenticated")]
    [InlineData("anonymous and role \"Guest\"")]
    [InlineData("(claim \"tier\" matches \"free\" or anonymous) and authenticated")]
    void should_refuse_the_combination(string condition)
    {
        var result = _compiler.Compile($"policy Anyone\n  require {condition}");
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AnonymousPolicyCombined);
    }
}
