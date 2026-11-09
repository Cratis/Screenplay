// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_remove_key_mapping;

public class and_the_target_has_a_dynamic_key : given.a_projection_removing_a_key
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("remove slots.$eventContent.slot"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_a_remove_key_mapping() => _result.Value!.Blocks.OfType<FromSyntax>().Single().Mappings.Single().ShouldBeOfExactType<RemoveKeyMappingSyntax>();
    [Fact] void should_keep_the_target() => _result.Value!.Blocks.OfType<FromSyntax>().Single().Mappings.Single().Property.ShouldEqual("slots.$eventContent.slot");
}
