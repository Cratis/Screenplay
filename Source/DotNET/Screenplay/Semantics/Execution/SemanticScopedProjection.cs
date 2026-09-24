// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Projects one fact through a scoped projection with the reference semantics of Chronicle's projection engine.
/// </summary>
/// <param name="plan">The execution plan.</param>
/// <param name="projection">The scoped projection.</param>
/// <param name="validator">The value validator.</param>
/// <param name="observed">The facts observed before the current one, used to back-fill joins.</param>
/// <remarks>
/// Instance removal deletes the instance; a child is upserted by identity (Chronicle <c>ProjectionEventContextExtensions.cs:180-202</c>);
/// a nested object is created on first touch, merged and cleared to null; a join updates every existing instance whose joined
/// property equals the joined event's source identity and never creates one, and a from event re-reads the latest joined event
/// (<c>ProjectionEventContextExtensions.cs:89-125</c>); <c>every</c> mappings run with the level's from and join events
/// (<c>ProjectionFactory.cs:589-701</c>). Constructs outside this are plan issues (<see cref="SemanticScopedProjectionIssues"/>).
/// </remarks>
internal sealed partial class SemanticScopedProjection(
    SemanticExecutionPlan plan,
    SemanticProjection projection,
    SemanticValueValidator validator,
    IReadOnlyList<SemanticFact> observed)
{
    readonly SemanticReadModel _readModel = plan.ReadModels[projection.ReadModel];
    readonly Dictionary<SemanticId, SemanticCompositeType> _types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
    readonly Dictionary<SemanticId, SemanticProperty> _properties = plan.ReadModels[projection.ReadModel].Properties
        .Concat(plan.Model.Application.Types.SelectMany(_ => _.Properties))
        .ToDictionary(_ => _.Id);
    readonly List<Document> _documents = [];
    SemanticFact _fact = null!;
    string? _failure;

    /// <summary>
    /// Projects one fact.
    /// </summary>
    /// <param name="instances">The world's read-model instances, updated in place when the fact projects.</param>
    /// <param name="fact">The fact.</param>
    /// <returns>The failure, or <see langword="null"/> when the fact projected.</returns>
    internal string? Apply(List<SemanticReadModelInstance> instances, SemanticFact fact)
    {
        _fact = fact;
        _documents.AddRange(instances
            .Where(_ => _.ReadModel == _readModel.Id)
            .Select(_ => new Document(_.Key, _.Values.ToDictionary(value => value.TargetProperty, value => value.Value))));
        var scope = projection.Scope!;
        var root = new Level([], [], Properties(_readModel.Properties));
        var handled = Process(scope, root);
        if (!handled && scope.Every is { SubscribesToAllEvents: true } every && _failure is null)
        {
            // 'all' covers event types no block names, keyed by the event source identity (ProjectionFactory.cs:616-631).
            if (EventSource() is { } key)
            {
                var document = RootDocument(key, true)!;
                Apply(document.State, every.Mappings, root.Targets, _fact);
            }
        }

        if (_failure is null)
        {
            Complete();
        }

        if (_failure is not null)
        {
            return _failure;
        }

        instances.RemoveAll(_ => _.ReadModel == _readModel.Id);
        instances.AddRange(_documents.Where(_ => !_.Removed).Select(_ => new SemanticReadModelInstance(
            _readModel.Id,
            _.Key,
            [.. _readModel.Properties.Select(property => new SemanticPropertyValue(property.Id, _.State.GetValueOrDefault(property.Id, SemanticValue.Null)))])));
        return null;
    }

    bool Process(SemanticProjectionScope scope, Level level)
    {
        var handled = false;
        foreach (var from in scope.From.Where(_ => _.EventContract == _fact.EventContract))
        {
            handled = true;
            ApplyFrom(from, scope, level);
        }

        foreach (var join in scope.Joins.Where(_ => _.EventContract == _fact.EventContract))
        {
            handled = true;
            ApplyJoin(join, scope, level);
        }

        foreach (var removal in scope.Removals.Where(_ => _.EventContract == _fact.EventContract))
        {
            handled = true;
            ApplyRemoval(removal, level);
        }

        foreach (var removal in scope.JoinRemovals.Where(_ => _.EventContract == _fact.EventContract))
        {
            handled = true;
            ApplyJoinRemoval(removal, level);
        }

        foreach (var children in scope.Children)
        {
            var element = _types[level.Targets[children.Property].Type.Target];
            var elementProperties = Properties(element.Properties);
            var identity = children.IdentifiedBy.IsSet
                ? children.IdentifiedBy
                : element.Properties.Single(_ => _.Name == _readModel.Properties.Single(property => property.IsIdentifier).Name).Id;
            handled |= Process(children.Scope, new([.. level.Address, new(children.Property, identity)], [], elementProperties));
        }

        foreach (var nested in scope.Nested)
        {
            var composite = _types[level.Targets[nested.Property].Type.Target];
            handled |= Process(nested.Scope, level with { Nested = [.. level.Nested, nested.Property], Targets = Properties(composite.Properties) });
        }

        return handled;
    }

    void ApplyFrom(SemanticProjectionFrom from, SemanticProjectionScope scope, Level level)
    {
        if (Locate(level, from.Key, from.ParentKey, true) is not { } location)
        {
            return;
        }

        Modify(location, true, target =>
        {
            Apply(target, from.Mappings, level.Targets, _fact);
            Apply(target, scope.Every?.Mappings ?? [], level.Targets, _fact);
            if (level.Nested.IsEmpty)
            {
                BackfillJoins(target, from, scope, level);
            }
        });
    }

    // A join matches every existing instance whose joined property equals the joined event's source identity and never creates one.
    void ApplyJoin(SemanticProjectionJoin join, SemanticProjectionScope scope, Level level)
    {
        if ((join.Key is null ? EventSource() : Key(join.Key)) is not { } value)
        {
            return;
        }

        foreach (var location in Elements(level.Address).ToArray())
        {
            Modify(location with { Nested = level.Nested }, false, target =>
            {
                if (target.TryGetValue(join.On, out var on) && SemanticValueRules.AreEqual(on, value))
                {
                    Apply(target, join.Mappings, level.Targets, _fact);
                    Apply(target, scope.Every?.Mappings ?? [], level.Targets, _fact);
                }
            });
        }
    }

    // Chronicle re-reads the latest joined event when a from event projects (ProjectionFactory.SetupJoinsForFromDefinition,
    // ProjectionFactory.cs:736-784): at a projection's own level for joins on a property the from maps or on the key property,
    // inside children for joins on the identity.
    void BackfillJoins(Dictionary<SemanticId, SemanticValue> target, SemanticProjectionFrom from, SemanticProjectionScope scope, Level level)
    {
        var identifier = level.Address.IsEmpty ? _readModel.Properties.Single(_ => _.IsIdentifier).Id : level.Address[^1].IdentifiedBy;
        foreach (var join in scope.Joins.Where(join => join.On == identifier || (level.Address.IsEmpty && from.Mappings.Any(_ => _.Target.Length == 1 && _.Target[0] == join.On))))
        {
            if (!target.TryGetValue(join.On, out var on) || on is SemanticNullValue)
            {
                continue;
            }

            var joined = observed.LastOrDefault(candidate => candidate.EventContract == join.EventContract && MatchesJoinKey(join, candidate, on));
            if (joined is not null)
            {
                Apply(target, join.Mappings, level.Targets, joined);
            }
        }
    }

    bool MatchesJoinKey(SemanticProjectionJoin join, SemanticFact candidate, SemanticValue on)
    {
        var current = _fact;
        try
        {
            _fact = candidate;
            return (join.Key is null ? EventSource() : Key(join.Key)) is { } key && SemanticValueRules.AreEqual(key, on);
        }
        finally
        {
            _fact = current;
        }
    }

    // In a nested object a removal clears it back to null; at a projection's own level it deletes the instance; inside children
    // it removes one child (Chronicle ProjectionFactory.cs:277-295, 459-465).
    void ApplyRemoval(SemanticProjectionRemoval removal, Level level)
    {
        if (!level.Nested.IsEmpty)
        {
            if (Locate(level with { Nested = [] }, removal.Key, removal.ParentKey, false) is { } enclosing)
            {
                Modify(enclosing with { Nested = level.Nested[..^1] }, false, target => target[level.Nested[^1]] = SemanticValue.Null);
            }

            return;
        }

        if (level.Address.IsEmpty)
        {
            if (Key(removal.Key) is { } key && RootDocument(key, false) is { } document)
            {
                document.Removed = true;
            }

            return;
        }

        if (Locate(level, removal.Key, removal.ParentKey, false) is { } location)
        {
            RemoveElement(location);
        }
    }

    // 'remove via join' removes every child with the identity across all parents (Chronicle ProjectionFactory.cs:297-308).
    void ApplyJoinRemoval(SemanticProjectionJoinRemoval removal, Level level)
    {
        if (level.Address.IsEmpty || Key(removal.Key) is not { } key)
        {
            return;
        }

        foreach (var location in Elements(level.Address).Where(_ => SemanticValueRules.AreEqual(_.Steps[^1].Identity, key)).ToArray())
        {
            RemoveElement(location);
        }
    }

    Location? Locate(Level level, SemanticProjectionKey key, SemanticProjectionKey? parentKey, bool create)
    {
        if (Key(key) is not { } identity)
        {
            return null;
        }

        if (level.Address.IsEmpty)
        {
            return RootDocument(identity, create) is { } document ? new(document, [], level.Nested) : null;
        }

        if (Key(parentKey ?? SemanticProjectionKey.EventSourceIdentity) is not { } parentIdentity)
        {
            return null;
        }

        var parentAddress = level.Address[..^1];
        var parents = parentAddress.IsEmpty
            ? _documents.Where(_ => !_.Removed && SemanticValueRules.AreEqual(_.Key, parentIdentity)).Select(_ => new Location(_, [], [])).ToArray()
            : [.. Elements(parentAddress).Where(_ => SemanticValueRules.AreEqual(_.Steps[^1].Identity, parentIdentity))];
        if (parents.Length != 1)
        {
            // Chronicle defers a child event until its parent exists (KeyResolvers.cs:712-717); the reference evaluator has no deferral.
            Fail(parents.Length == 0
                ? $"Projection '{projection.Name}' has no parent for a child event; Chronicle defers it until the parent exists, which the reference evaluator does not model."
                : $"Projection '{projection.Name}' parent identity is ambiguous for a child event.");
            return null;
        }

        var step = level.Address[^1];
        return parents[0] with { Steps = [.. parents[0].Steps, new(step.Property, step.IdentifiedBy, identity)], Nested = level.Nested };
    }

    Document? RootDocument(SemanticValue key, bool create)
    {
        var document = _documents.Find(_ => !_.Removed && SemanticValueRules.AreEqual(_.Key, key));
        if (document is null && create)
        {
            // The instance key is the read model's identifier, as the document key is in Chronicle.
            // Collections start empty (Chronicle ProjectionFactory.CreateInitialState, ProjectionFactory.cs:329-340).
            var state = Seed(_readModel.Properties);
            state[_readModel.Properties.Single(_ => _.IsIdentifier).Id] = key;
            document = new(key, state);
            _documents.Add(document);
        }

        return document;
    }

    void Fail(string failure) => _failure ??= failure;

    Dictionary<SemanticId, SemanticValue> Seed(IEnumerable<SemanticProperty> properties) =>
        properties.Where(_ => _.Type.IsCollection).ToDictionary(_ => _.Id, _ => SemanticValue.Array([]));

    Dictionary<SemanticId, SemanticProperty> Properties(ImmutableArray<SemanticProperty> properties) => properties.ToDictionary(_ => _.Id);

    sealed class Document(SemanticValue key, Dictionary<SemanticId, SemanticValue> state)
    {
        public SemanticValue Key { get; } = key;

        public Dictionary<SemanticId, SemanticValue> State { get; } = state;

        public bool Removed { get; set; }

        public bool Touched { get; set; }
    }

    sealed record ChildStep(SemanticId Property, SemanticId IdentifiedBy);

    sealed record ElementStep(SemanticId Property, SemanticId IdentifiedBy, SemanticValue Identity);

    sealed record Level(ImmutableArray<ChildStep> Address, ImmutableArray<SemanticId> Nested, Dictionary<SemanticId, SemanticProperty> Targets);

    sealed record Location(Document Document, ImmutableArray<ElementStep> Steps, ImmutableArray<SemanticId> Nested);
}
