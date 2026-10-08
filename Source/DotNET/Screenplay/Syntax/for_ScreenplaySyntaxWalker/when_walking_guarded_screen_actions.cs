// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_guarded_screen_actions : Specification
{
    given.a_counting_walker _walker;
    ScreenGuardedActionSyntax _action;

    void Establish()
    {
        var location = SourceLocation.Start;
        _walker = new();
        var condition = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("failed", location), location);
        var alternative = new ScreenActionAlternativeSyntax(condition, "Retry", location) { Arguments = [new("id", "item.id", location)] };
        _action = new("Again", [alternative], location)
        {
            Otherwise = new(ScreenActionOtherwiseOutcome.Execute, "Release", location) { Arguments = [new("id", "item.id", location)] },
            Navigate = new("Details", null, location)
        };
    }

    void Because() => _walker.VisitScreenDirective(_action);

    [Fact] void should_visit_every_child_once() => _walker.Nodes.Count.ShouldEqual(8);
    [Fact] void should_visit_the_condition() => _walker.Nodes.OfType<ComparisonConditionSyntax>().Count().ShouldEqual(1);
    [Fact] void should_visit_both_sets_of_arguments() => _walker.Nodes.OfType<InteractionArgumentSyntax>().Count().ShouldEqual(2);
    [Fact] void should_visit_navigation() => _walker.Nodes.OfType<ScreenNavigateSyntax>().Single().Screen.ShouldEqual("Details");
}
