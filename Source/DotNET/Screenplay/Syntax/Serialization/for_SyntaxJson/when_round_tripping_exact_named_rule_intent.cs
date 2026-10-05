// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_exact_named_rule_intent
{
    [Fact]
    void should_preserve_pending_and_attached_wrappers_through_exact_transport_and_printing()
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse(Source());
        parsed.Success.ShouldBeTrue();
        var wire = SyntaxJson.Serialize(parsed.Value!);
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(wire);
        SyntaxJson.Serialize(restored).GetRawText().ShouldEqual(wire.GetRawText());
        restored.SourceOptions.ShouldEqual(SourceOptions.Exact);
        var rules = ((DeclarativeValidateSyntax)restored.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single()).Rules.ToArray();
        rules[0].Implementation!.Hints.Single().Text.ShouldEqual("Needs team-owned criteria");
        rules[0].File.ShouldBeNull();
        rules[0].Code.ShouldBeNull();
        rules[1].Implementation!.Hints.Single().Text.ShouldEqual("Preserve attached criteria");
        rules[1].File!.Path.ShouldEqual("Rules/Attached.cs");
        rules[1].Severity.ShouldEqual(ValidationSeverity.Warning);
        rules[1].Message.ShouldEqual("Invalid label");
        ((ExactNumber)((LiteralExpressionSyntax)rules[2].Value!).Value!).CanonicalText.ShouldEqual("9007199254740993");
        var printed = new ScreenplayPrinter().Print(restored);
        printed.StartsWith("numbers exact\n", StringComparison.Ordinal).ShouldBeTrue();
        var reparsed = compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(restored, reparsed.Value!).ShouldBeTrue();
    }

    [Theory]
    [InlineData("wrong wrapper kind")]
    [InlineData("blank hint")]
    [InlineData("builtin rule wrapper")]
    [InlineData("conflicting payloads")]
    void should_refuse_invalid_wrapped_rules_in_exact_transport(string change)
    {
        var syntax = new ScreenplayCompiler().Parse(Source()).Value!;
        var wire = JsonNode.Parse(SyntaxJson.Serialize(syntax).GetRawText())!;
        var rule = wire["modules"]![0]!["features"]![0]!["slices"]![0]!["commands"]![0]!["validations"]![0]!["rules"]![0]!;
        switch (change)
        {
            case "wrong wrapper kind": rule["implementation"] = JsonNode.Parse("{\"kind\":\"PathExpressionSyntax\",\"path\":\"Wrong\"}"); break;
            case "blank hint": rule["implementation"]!["hints"]![0]!["text"] = " "; break;
            case "builtin rule wrapper": rule["rule"] = "NotEmpty"; break;
            case "conflicting payloads":
                rule["file"] = JsonNode.Parse("{\"kind\":\"FileReferenceSyntax\",\"path\":\"A.cs\"}");
                rule["code"] = JsonNode.Parse("{\"kind\":\"CodeBlockSyntax\",\"language\":\"csharp\",\"code\":\"true\"}");
                break;
        }
        Catch.Exception(() => SyntaxJson.Deserialize(System.Text.Json.JsonSerializer.SerializeToElement(wire))).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    static string Source([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return File.ReadAllText(Path.Combine(directory!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "exact-named-rule-intent.play"));
    }
}
