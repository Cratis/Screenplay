// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_anonymous_policies;

public class and_anonymous_is_negated : given.a_compiler
{
    [Theory]
    [InlineData("not anonymous")]
    [InlineData("authenticated or not anonymous")]
    [InlineData("not (anonymous)")]
    void should_point_at_authenticated(string condition)
    {
        var result = _compiler.Compile($"policy Signed\n  require {condition}");
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.NegatedAnonymousPolicy);
    }
}
