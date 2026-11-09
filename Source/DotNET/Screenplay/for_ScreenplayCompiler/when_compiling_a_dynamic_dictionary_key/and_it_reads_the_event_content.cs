// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_dynamic_dictionary_key;

public class and_it_reads_the_event_content : given.a_projection_counting_by_key
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("countBySlot.$eventContent.slot.name"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_key_in_the_target() => _result.Value!.Blocks.OfType<AllSyntax>().Single().Mappings.Single().Property.ShouldEqual("countBySlot.$eventContent.slot.name");
}
