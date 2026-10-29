// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Retains child occurrences until their original parent key exists, following Chronicle's ResolveFutures loop.
/// </summary>
internal sealed partial class SemanticScopedProjection
{
    // Every mapping a deferred child applies on replay: its own from and every mappings, unless it is a removal, and the
    // 'every include children' mappings of the ancestors ApplyAncestors applies. Deferring and replay both run with the
    // deferred fact as the current fact, so both select those ancestors through the same predicate.
    bool ReadsOccurrence(DeferredChild deferred) =>
        (deferred.Removal is null ? deferred.From!.Mappings.Concat(deferred.Scope.Every?.Mappings ?? []) : [])
            .Concat(AncestorsAppliedForCurrentFact(deferred.Level).SelectMany(_ => _.Scope.Every!.Mappings))
            .Any(_ => _.Operation == SemanticProjectionOperation.Set && _.Source is SemanticProjectionEventContextValue context && string.Equals(context.Path, "occurred", StringComparison.Ordinal));

    // The single place a child is deferred: one that will read an occurrence its fact lacks fails now, so no pending state
    // is published for it.
    void Defer(DeferredChild deferred)
    {
        if (deferred.Fact.Occurred is null && ReadsOccurrence(deferred))
        {
            Fail($"Projection '{projection.Name}' reads occurred but this fact carries no occurrence time.");
            return;
        }

        pending.Add(deferred);
    }

    Location? LocateFrom(SemanticProjectionFrom from, SemanticProjectionScope scope, Level level)
    {
        if (level.Address.IsEmpty)
        {
            return Locate(level, from.Key, from.ParentKey, true);
        }

        if (Key(from.Key) is not { } identity || ParentIdentity(from.ParentKey, level) is not { } parent)
        {
            return null;
        }

        var location = LocateChild(level, identity, parent);
        if (location is null && _failure is null)
        {
            Defer(new(projection.Id, _fact, from, scope, level, identity, parent));
        }

        return location;
    }

    Location? LocateRemoval(SemanticProjectionRemoval removal, SemanticProjectionScope scope, Level level)
    {
        if (level.Address.IsEmpty)
        {
            return Locate(level, removal.Key, removal.ParentKey, false);
        }

        if (Key(removal.Key) is not { } identity || ParentIdentity(removal.ParentKey, level) is not { } parent)
        {
            return null;
        }

        var location = LocateChild(level, identity, parent);
        if (location is null && _failure is null)
        {
            Defer(new(projection.Id, _fact, null, scope, level, identity, parent, removal));
        }

        return location;
    }

    SemanticValue? ParentIdentity(SemanticProjectionKey? parentKey, Level level)
    {
        var ancestor = level.Ancestors[^1];

        // FindParentEvent treats a shared event as the parent event itself, then resolves the parent's key.
        var shared = ancestor.Scope.From.FirstOrDefault(candidate => candidate.EventContract == _fact.EventContract);
        return shared is null ? Key(parentKey ?? SemanticProjectionKey.EventSourceIdentity) : Key(shared.Key);
    }

    void ResolvePending()
    {
        var current = _fact;
        try
        {
            bool progressed;
            do
            {
                progressed = false;
                foreach (var deferred in pending.Where(candidate => candidate.Projection == projection.Id).ToArray())
                {
                    _fact = deferred.Fact;
                    var location = LocateChild(deferred.Level, deferred.Identity, deferred.ParentIdentity);
                    if (_failure is not null)
                    {
                        return;
                    }

                    if (location is null)
                    {
                        continue;
                    }

                    ApplyDeferred(deferred, location);
                    pending.Remove(deferred);
                    progressed = true;
                }
            }
            while (progressed && _failure is null);
        }
        finally
        {
            _fact = current;
        }
    }

    void ApplyDeferred(DeferredChild deferred, Location location)
    {
        ApplyAncestors(location, deferred.Level);
        if (deferred.Removal is not null)
        {
            if (deferred.Level.Nested.IsEmpty)
            {
                RemoveElement(location);
            }
            else
            {
                Modify(location with { Nested = deferred.Level.Nested[..^1] }, false, target => target[deferred.Level.Nested[^1]] = SemanticValue.Null);
            }

            return;
        }

        Modify(location, true, target =>
        {
            Apply(target, deferred.From!.Mappings, deferred.Level.Targets, deferred.Fact, deferred.Level.Nested.IsEmpty);
            Apply(target, deferred.Scope.Every?.Mappings ?? [], deferred.Level.Targets, deferred.Fact, deferred.Level.Nested.IsEmpty && deferred.Scope.Joins.IsEmpty);
            if (deferred.Level.Nested.IsEmpty)
            {
                BackfillJoins(target, deferred.From!, deferred.Scope, deferred.Level);
            }
        });
    }

    void ApplyAncestors(Location location, Level level)
    {
        foreach (var ancestor in AncestorsAppliedForCurrentFact(level))
        {
            var enclosing = location with { Steps = location.Steps[..ancestor.Level.Address.Length], Nested = ancestor.Level.Nested };
            Modify(enclosing, false, target => Apply(target, ancestor.Scope.Every?.Mappings ?? [], ancestor.Level.Targets, _fact, ancestor.Level.Nested.IsEmpty && ancestor.Scope.Joins.IsEmpty));
        }
    }

    // An ancestor's own handler already applied its every mappings for a shared event.
    IEnumerable<Ancestor> AncestorsAppliedForCurrentFact(Level level) =>
        (level.Ancestors.IsDefault ? [] : level.Ancestors).Where(ancestor => ancestor.Scope.Every is { IncludeChildren: true } && !HandlesOwnEvent(ancestor.Scope));

    bool HandlesOwnEvent(SemanticProjectionScope scope) =>
        scope.From.Any(from => from.EventContract == _fact.EventContract) ||
        scope.Joins.Any(join => join.EventContract == _fact.EventContract) ||
        scope.Removals.Any(removal => removal.EventContract == _fact.EventContract) ||
        scope.JoinRemovals.Any(removal => removal.EventContract == _fact.EventContract);
}
