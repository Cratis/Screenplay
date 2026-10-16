// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_desugaring_studio_text_rules : given.a_semantic_binder
{
    [Fact]
    void should_bind_the_shared_studio_and_first_class_forms_to_identical_bytes()
    {
        var values = new[] { "", "A", ".", "$", "\\", "/", "#", " ", "é雪", "\n", "-", "]", "😀" };
        foreach (var value in values)
        {
            foreach (var v1 in new[] { true, false })
            {
                var literal = LegacyTextRulePatterns.Escape(value, v1);
                Compare(@"^(?![\s\S]*" + literal + ")", "not contains", value);
                Compare("^(?!" + literal + (v1 ? "$" : @"(?![\s\S])") + ")", "!=", value);
                if (!v1)
                {
                    Compare("^" + literal + @"(?![\s\S])", "==", value);
                    Compare(literal + @"(?![\s\S])", "ends with", value);
                }
            }
        }
    }

    void Compare(string pattern, string spelling, string value)
    {
        var legacy = Bind(Source("matches " + StringLiteral.Quote(pattern)));
        var current = Bind(Source(spelling + " " + StringLiteral.Quote(value)));
        Assert.True(legacy.Success, string.Join(';', legacy.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.True(current.Success, string.Join(';', current.Diagnostics.Select(diagnostic => diagnostic.Message)));
        legacy.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DeprecatedStudioTextRulePattern && diagnostic.Severity == DiagnosticSeverity.Warning).ShouldEqual(1);
        SemanticModelSerializer.Serialize(legacy.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(current.Value!.Model));
        new ScreenplayCompiler().Parse(Source("matches " + StringLiteral.Quote(pattern))).Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single().Rule.ShouldEqual(ValidationRuleKind.Matches);
    }

    static string Source(string rule) => "concept Ref : String\n  validate\n    " + rule + " severity warning message \"Invalid reference\"\n";
}
