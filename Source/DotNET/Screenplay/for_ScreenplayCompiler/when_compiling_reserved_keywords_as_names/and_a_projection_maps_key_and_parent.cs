// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_a_projection_maps_key_and_parent : given.a_compiler
{
    const string Source =
        """
        projection Order => OrderReadModel
          from OrderPlaced
            key orderId
            @key = externalKey
            @parent = parentReference
        """;

    CompilationResult<ProjectionSyntax> _result;
    FromSyntax _from;

    void Because()
    {
        _result = _compiler.CompileProjection(Source);
        _from = _result.Value!.Blocks.OfType<FromSyntax>().Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_key_directive() => _from.Key.ShouldNotBeNull();
    [Fact] void should_not_have_a_parent_key() => _from.ParentKey.ShouldBeNull();
    [Fact] void should_map_both_escaped_targets() => _from.Mappings.Count().ShouldEqual(2);
    [Fact] void should_strip_the_escape_from_the_key_mapping() => _from.Mappings.Any(_ => _.Property == "key").ShouldBeTrue();
    [Fact] void should_strip_the_escape_from_the_parent_mapping() => _from.Mappings.Any(_ => _.Property == "parent").ShouldBeTrue();
}
