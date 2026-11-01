// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_checking_published_case_parameter_destinations : Specification
{
    const string Source = """
        module M
          feature F
            slice StateChange Pricing
              command ChangePrice
                product Uuid identifier
                price Decimal
                produces PriceChanged
                  for product
                  price = price
              event PriceChanged
                price Decimal
            slice Translate Publication
              direction outbound
              public event PricePublished
                price Decimal
              projection Publisher => PricePublished
                from PriceChanged
                  price = price
              specification Publishing
                parameter id Uuid
                case One id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                given PriceChanged
                  for case.id
                  price = 10.5
                then PricePublished
                  for case.id
                  price = 10.5
        """;

    Diagnostic[] _compatible;
    Diagnostic[] _incompatible;

    void Because()
    {
        _compatible = TypeDiagnostics(Source);

        // Keep the given source literal so the incompatible diagnostic proves typing of the published event alone.
        _incompatible = TypeDiagnostics(Source
            .Replace("parameter id Uuid", "parameter id Int", StringComparison.Ordinal)
            .Replace("case One id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", "case One id = 1", StringComparison.Ordinal)
            .Replace("given PriceChanged\n          for case.id", "given PriceChanged\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", StringComparison.Ordinal));
    }

    [Fact] void should_accept_a_compatible_published_event_parameter() => _compatible.ShouldBeEmpty();
    [Fact] void should_type_the_published_event_parameter_from_its_folded_facts() => _incompatible.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.IncompatibleSpecificationParameterType);

    static Diagnostic[] TypeDiagnostics(string source) =>
        [.. new ScreenplayCompiler().Compile(source).Diagnostics
        .Where(diagnostic => diagnostic.Code == DiagnosticCodes.IncompatibleSpecificationParameterType || diagnostic.Code == DiagnosticCodes.OptionalSpecificationParameterTarget)];
}
