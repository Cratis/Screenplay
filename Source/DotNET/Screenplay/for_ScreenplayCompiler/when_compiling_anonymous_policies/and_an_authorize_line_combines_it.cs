// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_anonymous_policies;

public class and_an_authorize_line_combines_it : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("policy Anyone\n  require anonymous\npolicy Staff\n  require role \"Staff\"\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        authorize Anyone or Staff\n        id String identifier");

    [Fact] void should_refuse_the_composite_gate() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.AnonymousPolicyInCompositeGate);
}
