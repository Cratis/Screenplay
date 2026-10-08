// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_guarded_actions : Specification
{
    ScreenGuardedActionSyntax _original;
    ScreenGuardedActionSyntax _restored;

    void Establish()
    {
        var location = SourceLocation.Start;
        var condition = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("failed", location), location);
        var alternative = new ScreenActionAlternativeSyntax(condition, "Retry", location) { Arguments = [new("id", "item.id", location)] };
        _original = new("Again", [alternative], location)
        {
            Otherwise = new(ScreenActionOtherwiseOutcome.Execute, "Release", location) { Arguments = [new("id", "item.id", location)] },
            Navigate = new("Details", "id", location)
        };
    }

    void Because() => _restored = (ScreenGuardedActionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_original));

    [Fact] void should_preserve_structural_values() => SyntaxJson.StructurallyEqual(_original, _restored).ShouldBeTrue();
    [Fact] void should_keep_the_fallback_outcome_separate_from_the_discriminator() => SyntaxJson.Serialize(_restored.Otherwise!).GetProperty("outcome").GetString().ShouldEqual("Execute");
}
