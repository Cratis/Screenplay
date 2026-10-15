// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_generation_rule_bodies : given.a_printer
{
    [Theory]
    [InlineData("file Generators/Order.cs")]
    [InlineData("```csharp\n    return value;\n    ```")]
    void should_preserve_the_direct_body_in_print_parse_and_transport(string body)
    {
        var result = RoundTrip($"concept OrderId : String\n  generate rule Order\n    {body}");
        result.Original!.Diagnostics.ShouldBeEmpty();
        result.Reparsed.Diagnostics.ShouldBeEmpty();
        result.PrintedAgain.ShouldEqual(result.Printed);
        SyntaxJson.StructurallyEqual(result.Original.Value!, result.Reparsed.Value!).ShouldBeTrue();
        var json = SyntaxJson.Serialize(result.Original.Value!);
        SyntaxJson.StructurallyEqual(SyntaxJson.Deserialize(json), result.Original.Value!).ShouldBeTrue();
    }

    [Theory]
    [InlineData("file First.cs\n    file Second.cs", DiagnosticCodes.ConflictingImplementationSources)]
    [InlineData("file First.cs\n    ```csharp\n    return value;\n    ```", DiagnosticCodes.ConflictingImplementationSources)]
    [InlineData("implementation\n      file First.cs", DiagnosticCodes.UnknownRuleImplementationDirective)]
    [InlineData("file First.cs\n      unexpected", DiagnosticCodes.UnknownRuleImplementationDirective)]
    [InlineData("csharp\n      ```csharp\n      return value;\n      ```", DiagnosticCodes.UnknownRuleImplementationDirective)]
    void should_reject_extra_or_wrapped_body_sources(string body, string code)
    {
        var parsed = _compiler.Parse($"concept OrderId : String\n  generate rule Order\n    {body}");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();
    }

    [Fact]
    void should_not_treat_a_builtin_child_as_concept_provenance()
    {
        var parsed = _compiler.Parse("concept OrderId : String\n  generate uuid\n    file Wrong.cs");
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRuleImplementationDirective).ShouldBeTrue();
        parsed.Value!.Concepts.Single().File.ShouldBeNull();
    }

    [Fact]
    void should_refuse_conflicting_programmatic_payloads()
    {
        var generation = new GenerationSyntax("rule", SourceLocation.Start) { Rule = "Order", File = new("Order.cs", SourceLocation.Start), Code = new("csharp", "return value;", SourceLocation.Start) };
        Catch.Exception(() => SyntaxJson.Serialize(generation)).ShouldBeOfExactType<InvalidSyntaxJson>();
    }
}
