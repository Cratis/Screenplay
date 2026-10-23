// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Indexing;

internal static partial class SpecificationObligations
{
    static IEnumerable<(RemoveWithSyntax Removal, string Path, string? Variant)> Removals(IEnumerable<ProjectionBlockSyntax> blocks, string path = "", string? variant = null)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case RemoveWithSyntax removal:
                    yield return (removal, path, variant);
                    break;
                case ChildrenSyntax children:
                    foreach (var removal in Removals(children.Blocks, path.Length == 0 ? children.Property : path + "." + children.Property, variant)) yield return removal;
                    break;
                case NestedSyntax nested:
                    foreach (var removal in Removals(nested.Blocks, path.Length == 0 ? nested.Property : path + "." + nested.Property, variant)) yield return removal;
                    break;
                case ProjectionVariantSyntax value:
                    foreach (var removal in Removals(value.Blocks, path, value.Name)) yield return removal;
                    break;
            }
        }
    }

    static bool AssertsPath(PropertyMappingSyntax mapping, string path) => mapping.Property == path ||
        (path.StartsWith(mapping.Property + ".", StringComparison.Ordinal) && AssertsPath(mapping.Source, path[(mapping.Property.Length + 1)..]));

    static bool AssertsPath(ExpressionSyntax expression, string path) => expression switch
    {
        ObjectExpressionSyntax value => value.Members.Any(member => AssertsPath(new PropertyMappingSyntax(member.Name, member.Value, member.Location), path)),
        ListExpressionSyntax value => value.Items.Any(item => AssertsPath(item, path)),
        _ => false
    };

    static bool Authorized(AuthoringIndex index, ReadOwner owner) => index.Find(owner.Address, owner.Kind).Any(declaration => declaration.Syntax switch
    {
        ModuleSyntax module => module.Authorize is not null,
        FeatureSyntax feature => feature.Authorize is not null,
        _ => false
    });

    static bool ReactionOutcome(AuthoringIndex index, AuthoredDeclaration reaction, AuthoredDeclaration spec, SpecificationSyntax syntax)
    {
        var references = index.Outgoing(reaction.Owner).ToArray();
        var invoked = references.Where(reference => reference.Role == "invokes").Select(index.Resolve).Where(candidates => candidates.Length == 1).Select(candidates => candidates[0]);
        var events = references.Where(reference => reference.Role == "produces")
            .Concat(invoked.SelectMany(command => index.Outgoing(command.Owner).Where(reference => reference.Role == "produces")))
            .Select(index.Resolve).Where(candidates => candidates.Length == 1).Select(candidates => candidates[0]).Where(declaration => declaration.Kind == "Event").ToArray();

        return syntax.ThenEvents.Any(assertion => events.Any(@event => Resolves(index, spec, assertion.EventType, "Event", @event))) ||
            (syntax.ThenNoEvents && spec.Scope.SequenceEqual(reaction.Scope, StringComparer.Ordinal));
    }

    static bool DrivesEvent(AuthoringIndex index, AuthoredDeclaration spec, SpecificationSyntax syntax, AuthoredDeclaration @event)
    {
        if (syntax.WhenAppended is { } appended && Resolves(index, spec, appended.EventType, "Event", @event)) return true;
        if (syntax.When is not { } when) return false;
        var candidates = index.Resolve(new(when.CommandType, ["Command"], spec.Scope, when.Location, "review", spec.Owner));

        return candidates is [var command] && command.Syntax is CommandSyntax action && action.Produces.Any(production => Resolves(index, command, production.Event, "Event", @event));
    }

    static string? DeniedQuery(SpecificationSyntax syntax)
    {
        if (syntax.ThenDenied is null) return null;
        if (syntax.WhenQuery is { } performed) return performed.Query;
        var acted = new object?[] { syntax.When, syntax.WhenAppended, syntax.WhenClock, syntax.WhenTrigger, syntax.WhenCapture, syntax.WhenRedelivered }.Any(action => action is not null);

        // Like the semantic binder, a when-less denial acts through one query with no expected results.
        return !acted && syntax.ThenQueries.ToArray() is [var query] && !query.Results.Any() ? query.Query : null;
    }

    static bool QueryReturns(AuthoringIndex index, AuthoredDeclaration from, string name, AuthoredDeclaration model)
    {
        var queries = index.Resolve(new(name, ["Query"], from.Scope, from.Location, "review", from.Owner));

        return queries is [var query] && query.Syntax is QuerySyntax syntax && Resolves(index, query, syntax.ReturnType.Name, "ReadModel", model);
    }

    static bool CompetingClaim(AuthoringIndex index, AuthoredDeclaration spec, SpecificationSyntax syntax, AuthoredDeclaration constraint, UniquePropertyConstraintSyntax rule)
    {
        if (syntax.When is not { } when || !syntax.ThenErrors.Any(error => ((ConstraintSyntax)constraint.Syntax).Message is not { } message || error.Name == message)) return false;
        var commands = index.Resolve(new(when.CommandType, ["Command"], spec.Scope, when.Location, "review", spec.Owner));
        var events = index.Resolve(new(rule.Event, ["Event"], constraint.Scope, rule.Location, "review", constraint.Owner));
        if (commands is not [var command] || command.Syntax is not CommandSyntax commandSyntax || events is not [var @event]) return false;
        var destination = Literal(when.For, when.Values) ?? commandSyntax.Properties.Where(property => property.IsIdentifier).Select(property => Literal(new PathExpressionSyntax(property.Name, property.Location), when.Values)).FirstOrDefault(value => value is not null);
        if (destination is null) return false;
        var keys = new[] { rule.Property }.Concat(rule.AdditionalProperties).ToArray();

        return commandSyntax.Produces.Any(production => Resolves(index, command, production.Event, "Event", @event) &&
            syntax.Given.Any(given => Resolves(index, spec, given.EventType, "Event", @event) && Literal(given.For, given.Values) is { } previous && !Equals(previous, destination) && keys.All(key =>
                {
                    var claimed = Literal(given.Values.FirstOrDefault(mapping => mapping.Property == key)?.Source, given.Values);
                    var competing = Literal(production.Mappings.FirstOrDefault(mapping => mapping.Property == key)?.Source, when.Values);
                    return claimed is not null && Equals(claimed, competing);
                })));
    }

    static object? Literal(ExpressionSyntax? expression, IEnumerable<PropertyMappingSyntax> values) => expression switch
    {
        LiteralExpressionSyntax literal => literal.Value,
        PathExpressionSyntax path => values.FirstOrDefault(mapping => mapping.Property == path.Path)?.Source is LiteralExpressionSyntax literal ? literal.Value : null,
        _ => null
    };
}
