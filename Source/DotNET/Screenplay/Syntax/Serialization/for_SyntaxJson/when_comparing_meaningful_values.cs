// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_comparing_meaningful_values : Specification
{
    bool[] _equalities;

    void Because()
    {
        var location = SourceLocation.Start;
        var query = new QuerySyntax("All", new("Item", true, false, location), null, [], null, location);
        var specification = new SpecificationEventSyntax("Added", [], location);
        var eventNode = new EventSyntax("Added", [], location);
        var comparisons = new (SyntaxNode Left, SyntaxNode Right)[]
        {
            (query, query with { Scope = "global" }),
            (query, query with { IsObservable = true }),
            (query, query with { Name = "Other" }),
            (query, query with { Description = string.Empty }),
            (specification, specification with { For = new PathExpressionSyntax("id", location) }),
            (eventNode, eventNode with { File = new FileReferenceSyntax("Added.cs", location) }),
            (eventNode, eventNode with { Tags = [new(new LiteralExpressionSyntax("audit", location), location)] }),
            (new RawExpressionSyntax("a + b", location), new RawExpressionSyntax("a - b", location)),
            (new CodeBlockSyntax("csharp", "return 1;", location), new CodeBlockSyntax("csharp", "return 2;", location)),
            (new ClearMappingSyntax("status", location), new SetMappingSyntax("status", new LiteralExpressionSyntax(null, location), location)),
            (new ThemeSyntax("Theme", ["Web", "Mobile"], location), new ThemeSyntax("Theme", ["Mobile", "Web"], location)),
            (new LiteralExpressionSyntax(1, location), new LiteralExpressionSyntax(1d, location))
        };
        _equalities = [.. comparisons.Select(pair => SyntaxJson.StructurallyEqual(pair.Left, pair.Right))];
    }

    [Fact] void should_exercise_every_comparison() => _equalities.Length.ShouldEqual(12);
    [Fact] void should_detect_every_structural_difference() => _equalities.Any(equal => equal).ShouldBeFalse();
}
