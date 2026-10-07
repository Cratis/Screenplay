// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_slice_reads_a_read_model_built_elsewhere : given.a_model
{
    [Theory]
    [InlineData("readmodel View")]
    [InlineData("readmodel View\n      projection Builder => View\n        from E")]
    [InlineData("readmodel View\n      reducer Builder => View\n        on E")]
    [InlineData("readmodel View\n      projection Builder\n        variant View\n          enters on E\n          from E")]
    public void should_resolve_read_model_builders_variants_and_declaration_fallback(string declaration)
    {
        _graph = Graph("module M\n  feature F\n    slice StateChange Shape\n      readmodel View\n    slice StateView Builder\n      event E\n      " + declaration + "\n    slice StateChange Consumer\n      command C\n        reads View\n");
        _graph.Edges.Single(edge => edge.Kind == "decidesFrom").Producer.Address.ShouldEqual(declaration == "readmodel View" ? "M.F.Shape" : "M.F.Builder");
    }
}
