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
    Policy,
    Trigger,
    Property,
    Operation,
    System,
    EventSource,
    EventStream
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
                    var owner = domain switch
                    {
                        WorkspaceReferenceDomain.Property => WorkspaceStructuredReferences.Owner(entry, index),
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

    internal static IEnumerable<(string Member, WorkspaceReferenceDomain Domain)> Members(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        if (entry.Node is ProducesSyntax production && index.OwningSlice(entry) is { } slice && !index.Productions.IsEventProduction(production, slice))
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
        SpecificationEventSyntax => [("eventType", WorkspaceReferenceDomain.Event)],
        EventInteractionTriggerSyntax => [("eventName", WorkspaceReferenceDomain.Event)],
        ConcurrencySyntax => [("eventTypes", WorkspaceReferenceDomain.Event)],
        NamedTriggerSourceSyntax => [("name", WorkspaceReferenceDomain.Trigger)],
        _ => []
    };

    static IEnumerable<(string Member, WorkspaceReferenceDomain Domain)> OtherMembers(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index) => entry.Node switch
    {
        SpecificationStreamSyntax => [("eventSource", WorkspaceReferenceDomain.EventSource), ("stream", WorkspaceReferenceDomain.EventStream)],
        CommandStreamSyntax { PropertyCandidate: null } => [("eventSource", WorkspaceReferenceDomain.EventSource), ("stream", WorkspaceReferenceDomain.EventStream)],
        OperationSyntax => [("uses", WorkspaceReferenceDomain.System)],
        SpecificationOperationSyntax or SpecificationOperationFailureSyntax or SpecificationCompensatedSyntax => [("operation", WorkspaceReferenceDomain.Operation)],
        ObjectMemberSyntax => [("name", WorkspaceReferenceDomain.Property)],
        TypeRefSyntax => [("name", entry.Parent is { } parent && index.Find(parent)?.Node is QuerySyntax or ScreenDataSyntax
            ? WorkspaceReferenceDomain.View : WorkspaceReferenceDomain.Type)],
        CompositeKeySyntax => [("type", WorkspaceReferenceDomain.Type)],
        SpecificationCommandSyntax => [("commandType", WorkspaceReferenceDomain.Command)],
        InvokesSyntax or ScreenActionSyntax => [("command", WorkspaceReferenceDomain.Command)],
        FormSyntax => [("for", WorkspaceReferenceDomain.Command)],
        ReadsSyntax or ProjectionSyntax or ReducerSyntax => [("readModel", WorkspaceReferenceDomain.View)],
        SpecificationReadModelSyntax or SpecificationAbsentReadModelSyntax => [("name", WorkspaceReferenceDomain.View)],
        ScreenDataSyntax or FormPopulateViaQuerySyntax or SpecificationQuerySyntax => [("query", WorkspaceReferenceDomain.Query)],
        ScreenNavigateSyntax => [("screen", WorkspaceReferenceDomain.Screen)],
        PolicyReferenceSyntax => [("name", WorkspaceReferenceDomain.Policy)],
        PersonaSyntax => [("policies", WorkspaceReferenceDomain.Policy)],
        _ => []
    };
}
