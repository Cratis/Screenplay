// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_dynamic_dictionary_key_mapping : given.a_compiler
{
    const string Source =
        """
        projection EventStatistics => EventStatisticsReadModel
          all
            count eventCountByType.$eventContext.eventType.id
            increment processingAttempts.$eventContext.causationId
            decrement pendingItems.$eventContext.correlationId
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
    [Fact] void should_have_three_mappings() => _all.Mappings.Count().ShouldEqual(3);
    [Fact] void should_parse_count_with_dynamic_key() => _all.Mappings.OfType<CountMappingSyntax>().Single().Property.ShouldEqual("eventCountByType.$eventContext.eventType.id");
    [Fact] void should_parse_increment_with_dynamic_key() => _all.Mappings.OfType<IncrementMappingSyntax>().Single().Property.ShouldEqual("processingAttempts.$eventContext.causationId");
    [Fact] void should_parse_decrement_with_dynamic_key() => _all.Mappings.OfType<DecrementMappingSyntax>().Single().Property.ShouldEqual("pendingItems.$eventContext.correlationId");
}
