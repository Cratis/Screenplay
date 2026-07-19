// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_projection_with_qualified_name : given.a_compiler
{
    const string Source =
        """
        projection Core.Simulations.Simulation => Simulation
          from SimulationAdded
            name = name
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_qualified_name() => _result.Value!.Name.ShouldEqual("Core.Simulations.Simulation");
    [Fact] void should_have_the_read_model() => _result.Value!.ReadModel.ShouldEqual("Simulation");
}
