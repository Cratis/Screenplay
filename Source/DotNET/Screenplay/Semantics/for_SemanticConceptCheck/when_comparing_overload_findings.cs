// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_comparing_overload_findings : given.a_declaration
{
    [Theory]
    [InlineData("^z+$")]
    [InlineData("^[ab]{4}$")]
    void should_report_the_same_generated_pattern_rule(string pattern)
    {
        var syntax = SemanticConceptCheck.Check(Concept("String", new("nanoid", SourceLocation.Start) { Size = 4, Alphabet = "ab" }, Rule(ValidationRuleKind.Matches, pattern)), 2026);
        var semantic = SemanticConceptCheck.Check(syntax.Concept!, 2026);
        semantic.Findings.ToArray().ShouldEqual(syntax.Findings.ToArray());
    }
}
