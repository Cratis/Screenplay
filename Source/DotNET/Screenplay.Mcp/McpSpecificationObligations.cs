// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

static class McpSpecificationObligations
{
    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var selected = McpReviewSelection.Declarations(snapshot, arguments);
        var index = snapshot.Index;
        var expanded = snapshot.Compilation.Value is { } application ? SpecificationExamples.Expand(application) : null;
        if (expanded?.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) == true)
        {
            throw new McpFailure("SpecificationExampleExpansionFailed: obligations require unambiguous example expansion.");
        }
        var specifications = index.Declarations.Where(declaration => declaration.Syntax is SpecificationSyntax)
            .Select(declaration => (Declaration: declaration, Syntax: expanded?.Specifications.FirstOrDefault(value => value.Authored.Location == declaration.Location && value.Authored.Name == declaration.Name)?.Effective ?? (SpecificationSyntax)declaration.Syntax)).ToArray();
        var items = new List<object>();
        var document = McpJson.OptionalString(arguments, "document");

        void Add(string ruleId, McpDeclaration owner, SourceLocation location, string subject, string description, Func<McpDeclaration, SpecificationSyntax, bool> matches, string? reason = null)
        {
            if (document is not null && !owner.Locations.Any(value => value.Path == document)) return;
            reason ??= index.Readiness.ExecutionReadiness(owner.Syntax, null);
            var metBy = specifications.Where(value => matches(value.Declaration, value.Syntax)).Select(value => value.Declaration.Owner).ToArray();
            items.Add(new
            {
                ruleId,
                declaration = owner.Owner,
                location,
                subject,
                description,
                severity = reason is null ? "warning" : "info",
                reason,
                status = metBy.Length > 0 ? "met" : "unmet",
                specificationCount = metBy.Length,
                specifications = metBy.Take(20).ToArray(),
                specificationsTruncated = metBy.Length > 20
            });
        }

        bool Action(McpDeclaration spec, SpecificationSyntax syntax, McpDeclaration command) => syntax.When is { } when && McpReviewSelection.Resolves(index, spec, when.CommandType, "Command", command);
        bool Error(SpecificationSyntax syntax, string? message) => syntax.ThenErrors.Any(error => message is not null && error.Name == message);
        bool Asserted(SpecificationSyntax syntax) => syntax.ThenEvents.Any() || syntax.ThenReturns is not null || (syntax.ThenReadModels?.Any() ?? false) || syntax.ThenAbsentReadModels.Any() || syntax.ThenQueries.Any() || syntax.ThenResults.Any() || syntax.ThenNoResult is not null || syntax.ThenNoEvents;

        void Validation(McpDeclaration owner, IEnumerable<ValidateSyntax> validations, Func<McpDeclaration, SpecificationSyntax, bool> action, string prefix = "")
        {
            var ordinal = 0;
            foreach (var validation in validations)
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    Add("SPEC003", owner, validation.Location, prefix + $"validation[{ordinal++}]", "A rejection specification for the opaque validation block", (spec, syntax) => action(spec, syntax) && syntax.ThenErrors.Any(), "Inline validation code is not inspected; individual rules cannot be derived.");
                    continue;
                }
                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Add("SPEC002", owner, requirement.Location, prefix + $"require[{ordinal++}]", "A rejection specification for this require rule", (spec, syntax) => action(spec, syntax) && Error(syntax, requirement.Message), requirement.Message is null ? "No explicit rejection message identifies this rule; a generic then error cannot attribute coverage." : null);
                }
                foreach (var rule in declarative.Rules)
                {
                    Add("SPEC003", owner, rule.Location, prefix + $"{rule.Property}:{rule.Rule}[{ordinal++}]", "A rejection specification for this validation rule", (spec, syntax) => action(spec, syntax) && Error(syntax, rule.Message), rule.Message is null ? "No explicit rejection message identifies this rule; a generic then error cannot attribute coverage." : null);
                }
            }
        }

        foreach (var command in selected.Where(declaration => declaration.Syntax is CommandSyntax))
        {
            var syntax = (CommandSyntax)command.Syntax;
            Add("SPEC001", command, syntax.Location, "success", "A successful command specification with an outcome assertion", (spec, value) => Action(spec, value, command) && !value.ThenErrors.Any() && value.ThenDenied is null && Asserted(value));
            Validation(command, syntax.Validations, (spec, value) => Action(spec, value, command));
            if (syntax.Authorize is not null || command.Hierarchy.Any(owner => Authorized(index, owner)))
            {
                Add("SPEC004", command, syntax.Location, "denial", "An authorization denial specification for this command", (spec, value) => Action(spec, value, command) && value.ThenDenied is not null);
            }
        }
        foreach (var query in selected.Where(declaration => declaration.Syntax is QuerySyntax))
        {
            var syntax = (QuerySyntax)query.Syntax;
            if (syntax.Authorize is not null || query.Hierarchy.Any(owner => Authorized(index, owner)))
            {
                Add("SPEC004", query, syntax.Location, "denial", "An authorization denial specification for this query", (spec, value) => value.WhenQuery is { } when && McpReviewSelection.Resolves(index, spec, when.Query, "Query", query) && value.ThenDenied is not null);
            }
        }
        foreach (var concept in index.Declarations.Where(declaration => declaration.Syntax is ConceptSyntax))
        {
            var commands = selected.Where(declaration => declaration.Syntax is CommandSyntax command && command.Properties.Any(property => McpReviewSelection.Resolves(index, declaration, property.Type.Name, "Concept", concept))).ToArray();
            if (commands.Length == 0 && !selected.Contains(concept)) continue;
            Validation(concept, ((ConceptSyntax)concept.Syntax).Validations ?? [], (spec, value) => commands.Any(command => Action(spec, value, command)));
        }
        foreach (var model in selected.Where(declaration => declaration.Kind == "ReadModel"))
        {
            Add("SPEC006", model, model.Location, "view", "A specification asserting this read model", (spec, value) => (value.ThenReadModels ?? []).Any(view => McpReviewSelection.Resolves(index, spec, view.Name, "ReadModel", model)) || value.ThenAbsentReadModels.Any(view => McpReviewSelection.Resolves(index, spec, view.Name, "ReadModel", model)) ||
                value.ThenQueries.Any(query => QueryReturns(index, spec, query.Query, model)) || (value.WhenQuery is { } when && (value.ThenResults.Any() || value.ThenNoResult is not null) && QueryReturns(index, spec, when.Query, model)));
        }
        foreach (var projection in selected.Where(declaration => declaration.Kind == "Projection"))
        {
            var models = index.Outgoing(projection.Owner).Where(reference => reference.Role == "builds" || reference.Role == "buildsVariant").SelectMany(index.Resolve).Where(declaration => declaration.Kind == "ReadModel").ToArray();
            foreach (var removal in index.Outgoing(projection.Owner).Where(reference => reference.Role == "remove"))
            {
                var events = index.Resolve(removal);
                Add("SPEC007", projection, removal.Location, removal.Name, "A removal specification asserting an absent instance after the removal event", (spec, value) => events is [var @event] && value.WhenAppended is { } appended && McpReviewSelection.Resolves(index, spec, appended.EventType, "Event", @event) && value.ThenAbsentReadModels.Any(view => models.Any(model => McpReviewSelection.Resolves(index, spec, view.Name, "ReadModel", model))));
            }
        }
        foreach (var reaction in selected.Where(declaration => declaration.Syntax is ReactionSyntax))
        {
            var syntax = (ReactionSyntax)reaction.Syntax;
            var triggers = index.Outgoing(reaction.Owner).Where(reference => reference.Role == "trigger").SelectMany(index.Resolve).ToArray();
            Add("SPEC008", reaction, syntax.Location, "reaction", "A specification driving this reaction with an outcome assertion", (spec, value) => Asserted(value) && ((value.WhenRedelivered is { } redelivery && McpReviewSelection.Resolves(index, spec, redelivery.Reaction, "Reaction", reaction)) ||
                triggers.Any(trigger => (value.WhenAppended is { } appended && trigger.Kind == "Event" && McpReviewSelection.Resolves(index, spec, appended.EventType, "Event", trigger)) || (value.WhenTrigger is { } when && trigger.Kind == "Trigger" && McpReviewSelection.Resolves(index, spec, when.Trigger, "Trigger", trigger))) ||
                (value.WhenClock is not null && spec.Scope.SequenceEqual(reaction.Scope, StringComparer.Ordinal) && syntax.Triggers.Any(trigger => trigger.Source is IntervalTriggerSourceSyntax or ScheduleTriggerSourceSyntax))));
        }
        foreach (var constraint in index.Declarations.Where(declaration => declaration.Syntax is ConstraintSyntax))
        {
            var rules = new[] { (ConstraintSyntax)constraint.Syntax }.Concat(((ConstraintSyntax)constraint.Syntax).AdditionalRules).OfType<UniquePropertyConstraintSyntax>().ToArray();
            var events = index.Outgoing(constraint.Owner).Where(reference => reference.Role == "uniqueProperty").SelectMany(index.Resolve).ToArray();
            if (!selected.Contains(constraint) && !events.Any(@event => selected.Contains(@event))) continue;
            foreach (var rule in rules)
            {
                Add("SPEC005", constraint, rule.Location, rule.Event + ":" + string.Join(',', new[] { rule.Property }.Concat(rule.AdditionalProperties)), "A competing claim specification: prior equal claim, a different destination and rejection", (spec, value) => CompetingClaim(index, spec, value, constraint, rule));
            }
        }

        return McpReviewSelection.Page(snapshot, items, arguments, "Authored specification presence only, not execution or full behavioral coverage. Rules with explicit messages match exact then error text; unidentifiable rejection rules remain info/unmet. Matching specification lists are capped at 20; declaration-details pages the full inventory. Ambiguous references never establish a match.");
    }

    static bool Authorized(McpSyntaxIndex index, McpReadOwner owner) => index.Find(owner.Address, owner.Kind).Any(declaration => declaration.Syntax switch
    {
        ModuleSyntax module => module.Authorize is not null,
        FeatureSyntax feature => feature.Authorize is not null,
        _ => false
    });

    static bool QueryReturns(McpSyntaxIndex index, McpDeclaration from, string name, McpDeclaration model)
    {
        var queries = index.Resolve(new(name, ["Query"], from.Scope, from.Location, "review", from.Owner));

        return queries is [var query] && query.Syntax is QuerySyntax syntax && McpReviewSelection.Resolves(index, query, syntax.ReturnType.Name, "ReadModel", model);
    }

    static bool CompetingClaim(McpSyntaxIndex index, McpDeclaration spec, SpecificationSyntax syntax, McpDeclaration constraint, UniquePropertyConstraintSyntax rule)
    {
        if (syntax.When is not { } when || !syntax.ThenErrors.Any(error => ((ConstraintSyntax)constraint.Syntax).Message is not { } message || error.Name == message)) return false;
        var commands = index.Resolve(new(when.CommandType, ["Command"], spec.Scope, when.Location, "review", spec.Owner));
        var events = index.Resolve(new(rule.Event, ["Event"], constraint.Scope, rule.Location, "review", constraint.Owner));
        if (commands is not [var command] || command.Syntax is not CommandSyntax commandSyntax || events is not [var @event]) return false;
        var destination = Literal(when.For, when.Values) ?? commandSyntax.Properties.Where(property => property.IsIdentifier).Select(property => Literal(new PathExpressionSyntax(property.Name, property.Location), when.Values)).FirstOrDefault(value => value is not null);
        if (destination is null) return false;
        var keys = new[] { rule.Property }.Concat(rule.AdditionalProperties).ToArray();

        return commandSyntax.Produces.Any(production => McpReviewSelection.Resolves(index, command, production.Event, "Event", @event) &&
            syntax.Given.Any(given => McpReviewSelection.Resolves(index, spec, given.EventType, "Event", @event) && Literal(given.For, given.Values) is { } previous && !Equals(previous, destination) && keys.All(key =>
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
