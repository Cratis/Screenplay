// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

enum WorkspaceReferenceDomain
{
    Type,
    Event,
    Command,
    View,
    Query,
    Screen,
    DialogTemplate,
    Policy,
    Persona,
    Trigger,
    Property,
    Operation,
    System,
    EventSource,
    EventStream,
    Fixture,
    Reaction,
    Constraint,
    Container,
    SpecificationParameter
}

sealed record WorkspaceReferenceMember(WorkspaceSyntaxEntry Entry, string Member, int? Index, string Text, WorkspaceReferenceDomain Domain, string? Owner = null)
{
    internal string Path => $"{Entry.Handle.Path}/{Member}{(Index is { } index ? $"/{index}" : string.Empty)}";
    internal string Key => $"{Entry.Handle.Document}:{Path}";
}

static class WorkspaceReferenceMembers
{
    internal static IEnumerable<WorkspaceReferenceMember> All(WorkspaceSyntaxIndex index)
    {
        foreach (var entry in index.Entries)
        {
            foreach (var (member, domain) in Members(entry, index))
            {
                var property = entry.Node.GetType().GetProperty(char.ToUpperInvariant(member[0]) + member[1..])!;
                var value = property.GetValue(entry.Node);
                if (value is string text && text.Length > 0)
                {
                    if (IsBehaviorParameter(entry, index, text)) continue;
                    var owner = domain switch
                    {
                        WorkspaceReferenceDomain.Property => WorkspaceStructuredReferences.Owner(entry, index),
                        WorkspaceReferenceDomain.SpecificationParameter => TableOwner(entry, index),
                        WorkspaceReferenceDomain.EventStream when entry.Node is CommandStreamSyntax route => route.EventSource,
                        WorkspaceReferenceDomain.EventStream when entry.Node is SpecificationStreamSyntax route => route.EventSource,
                        _ => null
                    };
                    if (domain != WorkspaceReferenceDomain.Property || owner is not null)
                    {
                        yield return new(entry, member, null, text, domain, owner);
                    }
                }
                else if (value is IEnumerable values and not string)
                {
                    var position = 0;
                    foreach (var item in values)
                    {
                        if (item is string name)
                        {
                            yield return new(entry, member, position, name, domain);
                        }

                        position++;
                    }
                }
            }
        }
    }

    internal static string? TableOwner(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        for (var current = entry; current is not null; current = current.Parent is { } parent ? index.Find(parent) : null)
        {
            if (current.Node is SpecificationSyntax) return WorkspaceReferenceBindings.Key(current);
        }

        return null;
    }

    // Move-only operands: parameters are bound at their uses site, never as declaration names.
    internal static IEnumerable<WorkspaceReferenceMember> Interactions(WorkspaceSyntaxIndex index)
    {
        var parameters = new Dictionary<(string Behavior, string Parameter), HashSet<WorkspaceReferenceDomain>>();
        foreach (var entry in index.Entries)
        {
            var operand = entry.Node switch
            {
                ExecuteCommandActionSyntax execute => new WorkspaceReferenceMember(entry, "command", null, execute.Command, WorkspaceReferenceDomain.Command),
                NavigateActionSyntax navigate => new WorkspaceReferenceMember(entry, "screen", null, navigate.Screen, WorkspaceReferenceDomain.Screen),
                RefreshQueryActionSyntax refresh => new WorkspaceReferenceMember(entry, "query", null, refresh.Query, WorkspaceReferenceDomain.Query),
                _ => null
            };
            if (operand is null) continue;
            var behavior = Parents(entry, index).Select(parent => parent.Node).OfType<BehaviorSyntax>().FirstOrDefault();
            if (behavior?.Name is { } name && behavior.Parameters.Any(parameter => parameter.Name == operand.Text))
            {
                var key = (name, operand.Text);
                if (!parameters.TryGetValue(key, out var domains)) parameters[key] = domains = [];
                domains.Add(operand.Domain);
                continue;
            }
            yield return operand;
        }
        foreach (var entry in index.Entries.Where(entry => entry.Node is BehaviorArgumentSyntax))
        {
            var argument = (BehaviorArgumentSyntax)entry.Node;
            var uses = Parents(entry, index).Select(parent => parent.Node).OfType<UsesBehaviorSyntax>().First();
            if (!parameters.TryGetValue((uses.Behavior, argument.Name), out var domains)) continue;
            if (domains.Count != 1) throw new InvalidWorkspaceAuthoring($"Interaction argument '{uses.Behavior}.{argument.Name}' at '{entry.Handle}' has multiple target domains; its move continuity cannot be proven.");
            yield return new(entry, "value", null, argument.Value, domains.Single());
        }
    }

    internal static IEnumerable<WorkspaceSyntaxEntry> Parents(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        while (entry.Parent is { } handle && index.Find(handle) is { } parent)
        {
            yield return parent;
            entry = parent;
        }
    }

    internal static IEnumerable<(string Member, WorkspaceReferenceDomain Domain)> Members(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        var parent = entry.Parent is { } handle ? index.Find(handle) : null;
        if (entry.Node is ProducesSyntax production && parent?.Node is not InvocationRefusalSyntax && index.OwningSlice(entry) is { } slice && !index.Productions.IsEventProduction(production, slice))
            return [("event", WorkspaceReferenceDomain.Operation)];

        return EventMembers(entry.Node).Concat(OtherMembers(entry, index));
    }

    // The event-name catalog shared by repair consumer detection, rename and reference validation.
    // The syntax index recursively visits additional constraint rules, projection children/nested/variants,
    // reaction productions, capture appends, specification occurrences and screen/behavior interactions.
    // Policies have no typed event member; their code/files (like imports) go through WorkspaceOpaqueText.
    static IEnumerable<(string Member, WorkspaceReferenceDomain Domain)> EventMembers(SyntaxNode node) => node switch
    {
        UniquePropertyConstraintSyntax or UniqueEventConstraintSyntax => [("event", WorkspaceReferenceDomain.Event), ("releasedBy", WorkspaceReferenceDomain.Event)],
        ConstraintSyntax => [("releasedBy", WorkspaceReferenceDomain.Event)],
        ProducesSyntax or SeedEventSyntax or EventSpecSyntax or JoinEventSyntax or ClearWithSyntax or RemoveWithSyntax or
            RemoveViaJoinSyntax or ProjectionEntersOnSyntax or CaptureAppendSyntax or ReducerRuleSyntax => [("event", WorkspaceReferenceDomain.Event)],
        SpecificationEventSyntax => [("eventType", WorkspaceReferenceDomain.Fixture)],
        SpecificationRedeliverySyntax => [("eventType", WorkspaceReferenceDomain.Event)],
        EventInteractionTriggerSyntax => [("eventName", WorkspaceReferenceDomain.Event)],
        ConcurrencySyntax => [("eventTypes", WorkspaceReferenceDomain.Event)],
        NamedTriggerSourceSyntax => [("name", WorkspaceReferenceDomain.Trigger)],
        _ => []
    };

    static bool IsBehaviorParameter(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index, string text)
    {
        if (entry.Node is not InteractionActionSyntax) return false;
        for (var parent = entry.Parent; parent is not null; parent = index.Find(parent)?.Parent)
        {
            if (index.Find(parent)?.Node is BehaviorSyntax { Name: not null } behavior)
            {
                return behavior.Parameters.Any(parameter => parameter.Name == text);
            }
        }

        return false;
    }

    static IEnumerable<(string Member, WorkspaceReferenceDomain Domain)> OtherMembers(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index) => entry.Node switch
    {
        CaseValueExpressionSyntax => [("parameter", WorkspaceReferenceDomain.SpecificationParameter)],
        PropertyMappingSyntax when entry.Parent is { } caseParent && index.Find(caseParent)?.Node is SpecificationCaseSyntax => [("property", WorkspaceReferenceDomain.SpecificationParameter)],
        DependsOnSyntax => [("target", WorkspaceReferenceDomain.Container)],
        SpecificationStreamSyntax => [("eventSource", WorkspaceReferenceDomain.EventSource), ("stream", WorkspaceReferenceDomain.EventStream)],
        CommandStreamSyntax { PropertyCandidate: null } => [("eventSource", WorkspaceReferenceDomain.EventSource), ("stream", WorkspaceReferenceDomain.EventStream)],
        OperationSyntax => [("uses", WorkspaceReferenceDomain.System)],
        InvocationRefusalSyntax => [("constraint", WorkspaceReferenceDomain.Constraint)],
        SpecificationRedeliverySyntax => [("reaction", WorkspaceReferenceDomain.Reaction)],
        SpecificationOperationSyntax or SpecificationOperationFailureSyntax or SpecificationCompensatedSyntax => [("operation", WorkspaceReferenceDomain.Operation)],
        ObjectMemberSyntax => [("name", WorkspaceReferenceDomain.Property)],
        TypeRefSyntax => [("name", entry.Parent is { } parent && index.Find(parent)?.Node is QuerySyntax or ScreenDataSyntax
            ? WorkspaceReferenceDomain.View : WorkspaceReferenceDomain.Type)],
        CompositeKeySyntax => [("type", WorkspaceReferenceDomain.Type)],
        SpecificationExampleSyntax => [("type", WorkspaceReferenceDomain.Fixture)],
        SpecificationCommandSyntax => [("commandType", WorkspaceReferenceDomain.Fixture)],
        InvokesSyntax or ScreenActionSyntax or ScreenActionAlternativeSyntax or ScreenActionOtherwiseSyntax or ExecuteCommandActionSyntax => [("command", WorkspaceReferenceDomain.Command)],
        FormSyntax => [("for", WorkspaceReferenceDomain.Command)],
        ReadsSyntax or ProjectionSyntax or ReducerSyntax => [("readModel", WorkspaceReferenceDomain.View)],
        SpecificationReadModelSyntax => [("name", WorkspaceReferenceDomain.Fixture)],
        SpecificationAbsentReadModelSyntax => [("name", WorkspaceReferenceDomain.View)],
        ScreenDataSyntax or FormPopulateViaQuerySyntax or SpecificationQuerySyntax => [("query", WorkspaceReferenceDomain.Query)],
        ScreenNavigateSyntax or NavigateActionSyntax => [("screen", WorkspaceReferenceDomain.Screen)],
        OpenDialogActionSyntax => [("dialogTemplate", WorkspaceReferenceDomain.DialogTemplate)],
        RefreshQueryActionSyntax => [("query", WorkspaceReferenceDomain.Query)],
        RaiseTriggerActionSyntax => [("trigger", WorkspaceReferenceDomain.Trigger)],
        PolicyReferenceSyntax => [("name", WorkspaceReferenceDomain.Policy)],
        PersonaSyntax => [("policies", WorkspaceReferenceDomain.Policy)],
        SpecificationCallerPersonaSyntax => [("name", WorkspaceReferenceDomain.Persona)],
        _ => []
    };
}
