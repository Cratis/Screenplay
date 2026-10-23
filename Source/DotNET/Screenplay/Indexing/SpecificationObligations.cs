// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Indexing;

internal static partial class SpecificationObligations
{
    internal static List<SpecificationObligation> Evaluate(AuthoringSnapshot snapshot, AuthoredDeclaration[]? selection = null, string? document = null)
    {
        var selected = selection ?? [.. snapshot.Index.Declarations];
        var index = snapshot.Index;
        var expanded = snapshot.Compilation.Value is { } application ? SpecificationExamples.Expand(application) : null;
        if (expanded?.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code != DiagnosticCodes.UnsynthesizablePersonaCaller) == true)
        {
            throw new InvalidWorkspaceAuthoring("SpecificationExampleExpansionFailed: obligations require unambiguous example expansion.");
        }
        var specifications = index.Declarations.Where(declaration => declaration.Syntax is SpecificationSyntax)
            .Select(declaration => (Declaration: declaration, Syntax: expanded?.Specifications.FirstOrDefault(value => value.Authored.Location == declaration.Location && value.Authored.Name == declaration.Name)?.Effective ?? (SpecificationSyntax)declaration.Syntax)).ToArray();
        var items = new List<SpecificationObligation>();

        void Add(string ruleId, AuthoredDeclaration owner, SourceLocation location, string subject, string description, Func<AuthoredDeclaration, SpecificationSyntax, bool> matches, string? reason = null)
        {
            if (document is not null && !owner.Locations.Any(value => value.Path == document)) return;
            reason ??= index.Readiness.ExecutionReadiness(owner.Syntax, null);
            var metBy = specifications.Where(value => matches(value.Declaration, value.Syntax)).Select(value => value.Declaration.Owner).ToArray();
            items.Add(new(ruleId, owner.Owner, location, subject, description, reason, metBy));
        }

        bool Action(AuthoredDeclaration spec, SpecificationSyntax syntax, AuthoredDeclaration command) => syntax.When is { } when && Resolves(index, spec, when.CommandType, "Command", command);
        bool Error(SpecificationSyntax syntax, string? message) => syntax.ThenErrors.Any(error => message is not null && error.Name == message);
        bool Asserted(SpecificationSyntax syntax) => syntax.ThenEvents.Any() || syntax.ThenReturns is not null || (syntax.ThenReadModels?.Any() ?? false) || syntax.ThenAbsentReadModels.Any() || syntax.ThenQueries.Any() || syntax.ThenResults.Any() || syntax.ThenNoResult is not null || syntax.ThenNoEvents;

        void Validation(AuthoredDeclaration owner, IEnumerable<ValidateSyntax> validations, Func<AuthoredDeclaration, SpecificationSyntax, bool> action, string prefix = "")
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
                Add("SPEC004", query, syntax.Location, "denial", "An authorization denial specification for this query", (spec, value) => DeniedQuery(value) is { } name && Resolves(index, spec, name, "Query", query));
            }
        }
        foreach (var concept in index.Declarations.Where(declaration => declaration.Syntax is ConceptSyntax))
        {
            var commands = index.Declarations.Where(declaration => declaration.Syntax is CommandSyntax command && command.Properties.Any(property => Resolves(index, declaration, property.Type.Name, "Concept", concept))).ToArray();
            if (!commands.Any(selected.Contains) && !selected.Contains(concept)) continue;
            Validation(concept, ((ConceptSyntax)concept.Syntax).Validations ?? [], (spec, value) => commands.Any(command => Action(spec, value, command)));
        }
        foreach (var model in selected.Where(declaration => declaration.Kind == "ReadModel"))
        {
            Add("SPEC006", model, model.Location, "view", "A specification asserting this read model", (spec, value) => (value.ThenReadModels ?? []).Any(view => Resolves(index, spec, view.Name, "ReadModel", model)) || value.ThenAbsentReadModels.Any(view => Resolves(index, spec, view.Name, "ReadModel", model)) ||
                (DeniedQuery(value) is null && value.ThenQueries.Any(query => QueryReturns(index, spec, query.Query, model))) || (value.WhenQuery is { } when && (value.ThenResults.Any() || value.ThenNoResult is not null) && QueryReturns(index, spec, when.Query, model)));
        }
        foreach (var projection in selected.Where(declaration => declaration.Kind == "Projection"))
        {
            var models = index.Outgoing(projection.Owner).Where(reference => reference.Role == "builds" || reference.Role == "buildsVariant").SelectMany(index.Resolve).Where(declaration => declaration.Kind == "ReadModel").ToArray();
            foreach (var (removal, path, variant) in Removals(((ProjectionSyntax)projection.Syntax).Blocks))
            {
                var events = index.Resolve(new(removal.Event, ["Event"], projection.Scope, removal.Location, "remove", projection.Owner));
                var affected = variant is null ? models : [.. models.Where(model => Resolves(index, projection, variant, "ReadModel", model))];
                var description = path.Length == 0
                    ? "A removal specification asserting an absent instance of the affected view after the removal event"
                    : $"A removal specification asserting the affected view's '{path}' collection or nested state after the removal event";
                Add("SPEC007", projection, removal.Location, removal.Event, description, (spec, value) => events is [var @event] && DrivesEvent(index, spec, value, @event) &&
                    (path.Length == 0
                        ? value.ThenAbsentReadModels.Any(view => affected.Any(model => Resolves(index, spec, view.Name, "ReadModel", model)))
                        : (value.ThenReadModels ?? []).Any(view => affected.Any(model => Resolves(index, spec, view.Name, "ReadModel", model)) && view.Properties.Any(property => AssertsPath(property, path)))));
            }
        }
        foreach (var reaction in selected.Where(declaration => declaration.Syntax is ReactionSyntax))
        {
            var syntax = (ReactionSyntax)reaction.Syntax;
            var triggers = index.Outgoing(reaction.Owner).Where(reference => reference.Role == "trigger").SelectMany(index.Resolve).ToArray();
            Add("SPEC008", reaction, syntax.Location, "reaction", "A specification driving this reaction with an outcome assertion", (spec, value) => ReactionOutcome(index, reaction, spec, value) && ((value.WhenRedelivered is { } redelivery && Resolves(index, spec, redelivery.Reaction, "Reaction", reaction)) ||
                triggers.Any(trigger => (trigger.Kind == "Event" && DrivesEvent(index, spec, value, trigger)) || (value.WhenTrigger is { } when && trigger.Kind == "Trigger" && Resolves(index, spec, when.Trigger, "Trigger", trigger))) ||
                (value.WhenTrigger is { } builtIn && (builtIn.Trigger == "Startup" || builtIn.Trigger == "Shutdown") && syntax.Triggers.Any(trigger => trigger.Source is NamedTriggerSourceSyntax named && named.Name == builtIn.Trigger)) ||
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

        return items;
    }

    internal static bool Resolves(AuthoringIndex index, AuthoredDeclaration from, string name, string kind, AuthoredDeclaration target)
    {
        var candidates = index.Resolve(new(name, [kind], from.Scope, from.Location, "review", from.Owner));
        return candidates is [var candidate] && candidate.Address == target.Address && candidate.Kind == target.Kind;
    }
}
