// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_all_block : given.a_compiler
{
    const string Source =
        """
        projection EventStatistics => EventStatisticsReadModel
          all
            count eventCount
            lastOccurred = $eventContext.occurred
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
    [Fact] void should_parse_the_all_block() => _all.ShouldNotBeNull();
    [Fact] void should_have_two_mappings() => _all.Mappings.Count().ShouldEqual(2);
    [Fact] void should_have_count_mapping() => _all.Mappings.OfType<CountMappingSyntax>().Single().Property.ShouldEqual("eventCount");
    [Fact] void should_have_set_mapping() => _all.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("lastOccurred");
}
