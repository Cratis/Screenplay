// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_set_once_mapping : given.a_compiler
{
    const string Source =
        """
        projection Models => ModelReadModel
          from ModelUpdated
            set once createdBy = $causedBy.subject
            set once createdAt to $eventContext.occurred
            set once firstName = name
            set once = name
        """;

    CompilationResult<ProjectionSyntax> _result;
    MappingSyntax[] _mappings;

    void Because()
    {
        _result = _compiler.CompileProjection(Source);
        _mappings = [.. _result.Value!.Blocks.OfType<FromSyntax>().Single().Mappings];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_equals_form() => _mappings[0].ShouldBeOfExactType<SetOnceMappingSyntax>();
    [Fact] void should_read_the_target() => _mappings[0].Property.ShouldEqual("createdBy");
    [Fact] void should_read_the_caused_by_value() => ((SetOnceMappingSyntax)_mappings[0]).Source.ShouldBeOfExactType<CausedByExpressionSyntax>();
    [Fact] void should_read_the_to_form() => _mappings[1].ShouldBeOfExactType<SetOnceMappingSyntax>();
    [Fact] void should_read_an_event_property_value() => ((SetOnceMappingSyntax)_mappings[2]).Source.ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_keep_a_property_named_once_a_plain_set() => _mappings[3].ShouldBeOfExactType<SetMappingSyntax>();
}
