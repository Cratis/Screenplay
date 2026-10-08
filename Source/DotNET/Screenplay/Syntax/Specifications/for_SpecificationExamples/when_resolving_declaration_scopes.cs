// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_resolving_declaration_scopes : Specification
{
    const string Source =
        """
        module Billing
          feature Invoicing
            slice StateChange First
              event Recorded
                amount Int
              example Fact : Recorded
                amount = 10
            slice StateChange Second
              event Recorded
                amount Int
              specification SharingTheFirstFact
                given First.Fact
                then readmodel Balance
        """;

    EffectiveSpecificationApplication _result;

    void Because() => _result = SpecificationExamples.Expand(new ScreenplayCompiler().Parse(Source).Value!);

    [Fact] void should_keep_the_example_declaration_scope_instead_of_the_use_scope() => _result.Specifications[0].Effective.Given.Single().EventType.ShouldEqual("Billing.Invoicing.First.Recorded");
    [Fact] void should_resolve_a_qualified_reference_without_errors() => _result.Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_reject_an_ambiguous_type()
    {
        var source = "example Fact : Recorded\n  amount = 10\n" + Source.Replace("              example Fact : Recorded\n                amount = 10\n", string.Empty, StringComparison.Ordinal);
        SpecificationExamples.Expand(new ScreenplayCompiler().Parse(source).Value!).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType);
    }

    [Fact]
    void should_reject_a_historical_only_property()
    {
        const string source =
            """
            example Fact : Recorded
              removed = 10
            module Billing
              feature Invoicing
                slice StateChange Recording
                  event Recorded generation 1
                    removed Int
                  event Recorded generation 2
                    amount Int
            """;
        SpecificationExamples.Expand(new ScreenplayCompiler().Parse(source).Value!).Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSpecificationExampleValue);
    }
}
