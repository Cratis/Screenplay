// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_identity_without_a_path : given.a_compiler
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = [.. new[] { "$identity", "$identity." }.Select(expression => _compiler.Parse(
        $"module M\n  feature F\n    slice StateChange S\n      command C\n        produces E\n          caller = {expression}"))];

    [Fact] void should_reject_both_missing_paths() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_invalid_expression() => _results.All(result => result.Diagnostics.Single().Code == DiagnosticCodes.InvalidExpression).ShouldBeTrue();
}
