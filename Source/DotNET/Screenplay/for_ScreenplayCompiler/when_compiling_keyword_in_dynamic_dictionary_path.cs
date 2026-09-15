// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_keyword_in_dynamic_dictionary_path : given.a_compiler
{
    const string Source =
        """
        projection EventStatistics => EventStatisticsReadModel
          all
            count eventCountByType.$eventContext.eventType.id
        """;

    CompilationResult<ProjectionSyntax> _result;
    AllSyntax _all;

    void Because()
    {
        _result = _compiler.CompileProjection(Source);
        _all = _result.Value!.Blocks.OfType<AllSyntax>().Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_path_without_escaping() => _all.Mappings.OfType<CountMappingSyntax>().Single().Property.ShouldEqual("eventCountByType.$eventContext.eventType.id");
    [Fact] void should_not_require_mid_path_escaping_for_keyword_id() => _all.Mappings.OfType<CountMappingSyntax>().Single().Property.ShouldNotContain("@id");
}
