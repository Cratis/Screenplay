// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching.Specs.for_PortableMatcher;

public class when_breaking_internal_match_invariants : Specification
{
    [Fact]
    void should_report_an_invalid_automaton_expression() => Catch.Exception(() => _ = new MatchAutomatonBuilder().Compile(null!)).ShouldBeOfExactType<InvalidOperationException>();

    [Fact]
    void should_report_an_invalid_writer_expression() => Catch.Exception(() => _ = MatchExpressionWriter.DotNet(null!)).ShouldBeOfExactType<InvalidOperationException>();

    [Fact]
    void should_report_a_missing_expression_in_a_refused_pattern()
    {
        var refused = new PortableMatchParser("(?=a)").Parse();
        Catch.Exception(() => _ = refused.NormalizedDotNetPattern).ShouldBeOfExactType<InvalidOperationException>();
    }
}
