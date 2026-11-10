// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_classifying_syntax_references : Specification
{
    [Fact]
    void should_classify_every_string_member_exactly_once()
    {
        var pairs = typeof(SyntaxNode).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(SyntaxNode).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties().Where(property => property.PropertyType == typeof(string) || typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType))
                .Select(property => (Node: type, Property: property.Name))).ToArray();
        var classified = ReferenceKinds.Classified.ToArray();
        var missing = pairs.Except(classified).OrderBy(pair => pair.Node.Name).ThenBy(pair => pair.Property);
        var duplicates = classified.GroupBy(pair => pair).Where(group => group.Count() != 1).Select(group => group.Key);
        var stale = classified.Except(pairs);
        var failures = missing.Select(pair => $"Unclassified: {pair.Node.FullName}.{pair.Property}")
            .Concat(duplicates.Select(pair => $"Duplicate: {pair.Node.FullName}.{pair.Property}"))
            .Concat(stale.Select(pair => $"Stale: {pair.Node.FullName}.{pair.Property}"));
        Assert.False(failures.Any(), string.Join('\n', failures));
    }

    [Fact]
    void should_reject_an_unclassified_nullable_member_even_on_a_primitive_type_reference()
    {
        var node = new UnclassifiedTypeReference("String", false, false, new(0, 0, string.Empty));
        AgentReferenceCoverage.Complete(node, node).ShouldBeFalse();
    }

    [Fact]
    void should_accept_a_classified_primitive_type_reference()
    {
        var node = new TypeRefSyntax("String", false, false, new(0, 0, string.Empty));
        AgentReferenceCoverage.Complete(node, node).ShouldBeTrue();
        ReferenceKinds.For(node).ShouldBeEmpty();
    }

    sealed record UnclassifiedTypeReference(string Name, bool IsCollection, bool IsOptional, SourceLocation Location)
        : TypeRefSyntax(Name, IsCollection, IsOptional, Location)
    {
        public string? FutureReference { get; init; }
    }
}
