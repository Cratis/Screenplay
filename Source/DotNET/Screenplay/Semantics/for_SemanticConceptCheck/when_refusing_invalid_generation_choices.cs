// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_refusing_invalid_generation_choices : given.a_declaration
{
    [Theory]
    [InlineData("checksum")]
    [InlineData("case")]
    [InlineData("encoding")]
    [InlineData("rule")]
    [InlineData("range")]
    [InlineData("pattern")]
    void should_return_an_option_error_without_assessment(string option)
    {
        var generation = option switch
        {
            "checksum" => new GenerationSyntax("pattern", SourceLocation.Start) { Pattern = "#{8}", Checksum = "invalid" },
            "case" => new("uuid", SourceLocation.Start) { Case = "invalid" },
            "encoding" => new("bytes", SourceLocation.Start) { Count = 16, Encoding = "invalid" },
            "rule" => new("rule", SourceLocation.Start) { Rule = "not-a-name" },
            "range" => new("random", SourceLocation.Start) { Range = "invalid" },
            _ => new("pattern", SourceLocation.Start)
        };
        var result = SemanticConceptCheck.Check(Concept(option == "range" ? "Int" : "String", generation, option == "range" ? [] : [Rule(ValidationRuleKind.NotEmpty)]), 2026);
        var finding = result.Findings.Single();
        (finding.Code, finding.Severity, finding.Target, finding.Option, result.Support, result.Proofs.Length)
            .ShouldEqual((DiagnosticCodes.InvalidGenerationArgument, DiagnosticSeverity.Error, SemanticConceptFindingTarget.Option, option, (GenerationSupport?)null, 0));
    }
}
