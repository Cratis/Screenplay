// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

// #185 variants: independent read models and projections, update-only key joins and sibling removals.
public static partial class canonical_serialization_golden_vectors
{
    static SemanticSlice CreateVariants(ApplicationIdentity applicationIdentity, SemanticId uuidConcept, SemanticId textConcept)
    {
        var created = Event(applicationIdentity, 410, "IssueCreated", [(411, "IssueId", SemanticTypeReference.ForConcept(uuidConcept))]);
        var started = Event(applicationIdentity, 412, "IssueStarted", [(413, "IssueId", SemanticTypeReference.ForConcept(uuidConcept))]);
        var changed = Event(applicationIdentity, 414, "TitleChanged", [(415, "IssueId", SemanticTypeReference.ForConcept(uuidConcept)), (416, "Title", SemanticTypeReference.ForConcept(textConcept))]);
        var backlogId = Id(420);
        var developmentId = Id(422);
        var backlog = new SemanticReadModel(Id(430), "BacklogItem", [new(backlogId, "IssueId", SemanticTypeReference.ForConcept(uuidConcept), true), new(Id(421), "Title", SemanticTypeReference.ForConcept(textConcept, isOptional: true), false)]);
        var development = new SemanticReadModel(Id(431), "DevelopmentItem", [new(developmentId, "IssueId", SemanticTypeReference.ForConcept(uuidConcept), true), new(Id(423), "Title", SemanticTypeReference.ForConcept(textConcept, isOptional: true), false)]);
        var sharedKey = new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(415)]));
        SemanticProjectionJoin SharedJoin(SemanticId on, SemanticId title) =>
            new(changed.Id, on, [Map([title], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([Id(416)]))]) { Key = sharedKey };
        var backlogProjection = new SemanticProjection(Id(440), "8:WorkItem:BacklogItem", backlog.Id, [])
        {
            Scope = SemanticProjectionScope.Empty with
            {
                From = [new(created.Id, new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(411)])), null, [])],
                Joins = [SharedJoin(backlogId, Id(421))],
                Removals = [new(started.Id, SemanticProjectionKey.EventSourceIdentity, null)]
            }
        };
        var developmentProjection = new SemanticProjection(Id(441), "8:WorkItem:DevelopmentItem", development.Id, [])
        {
            Scope = SemanticProjectionScope.Empty with
            {
                From = [new(started.Id, new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([Id(413)])), null, [])],
                Joins = [SharedJoin(developmentId, Id(423))],
                Removals = [new(created.Id, SemanticProjectionKey.EventSourceIdentity, null)]
            }
        };
        return new(Id(450), "Variants", SemanticSliceKind.StateView, [created, started, changed], [], [backlog, development], [backlogProjection, developmentProjection], [], []);
    }
}
#endif
