// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_projection_level_key : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Orders\n  key orderId\n  from OrderPlaced key orderId\n    status = status");

    [Fact] void should_succeed_despite_the_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_on_the_projection_key() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnusedProjectionKey);
    [Fact] void should_retain_the_from_key() => _result.Value!.Blocks.OfType<FromSyntax>().Single().Events.Single().Key.ShouldNotBeNull();
}
