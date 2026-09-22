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
    Trigger
}

sealed record WorkspaceReferenceMember(WorkspaceSyntaxEntry Entry, string Member, int? Index, string Text, WorkspaceReferenceDomain Domain)
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
                    yield return new(entry, member, null, text, domain);
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

    internal static IEnumerable<(string Member, WorkspaceReferenceDomain Domain)> Members(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index) => entry.Node switch
    {
        TypeRefSyntax => [("name", entry.Parent is { } parent && index.Find(parent)?.Node is QuerySyntax or ScreenDataSyntax
            ? WorkspaceReferenceDomain.View : WorkspaceReferenceDomain.Type)],
        CompositeKeySyntax => [("type", WorkspaceReferenceDomain.Type)],
        ProducesSyntax or SeedEventSyntax or UniquePropertyConstraintSyntax or UniqueEventConstraintSyntax or EventSpecSyntax or JoinEventSyntax or
            ClearWithSyntax or RemoveWithSyntax or RemoveViaJoinSyntax or ProjectionEntersOnSyntax or CaptureAppendSyntax or ReducerRuleSyntax => [("event", WorkspaceReferenceDomain.Event)],
        SpecificationEventSyntax => [("eventType", WorkspaceReferenceDomain.Event)],
        ConcurrencySyntax => [("eventTypes", WorkspaceReferenceDomain.Event)],
        SpecificationCommandSyntax => [("commandType", WorkspaceReferenceDomain.Command)],
        InvokesSyntax or ScreenActionSyntax => [("command", WorkspaceReferenceDomain.Command)],
        FormSyntax => [("for", WorkspaceReferenceDomain.Command)],
        ReadsSyntax or ProjectionSyntax or ReducerSyntax => [("readModel", WorkspaceReferenceDomain.View)],
        SpecificationReadModelSyntax => [("name", WorkspaceReferenceDomain.View)],
        ScreenDataSyntax or FormPopulateViaQuerySyntax or SpecificationQuerySyntax => [("query", WorkspaceReferenceDomain.Query)],
        ScreenNavigateSyntax => [("screen", WorkspaceReferenceDomain.Screen)],
        PolicyReferenceSyntax => [("name", WorkspaceReferenceDomain.Policy)],
        PersonaSyntax => [("policies", WorkspaceReferenceDomain.Policy)],
        NamedTriggerSourceSyntax => [("name", WorkspaceReferenceDomain.Trigger)],
        _ => []
    };
}
