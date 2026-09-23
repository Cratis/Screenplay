// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Navigates and changes the instance tree of a scoped projection.
/// </summary>
internal sealed partial class SemanticScopedProjection
{
    static ImmutableArray<SemanticValue> Elements(Dictionary<SemanticId, SemanticValue> container, SemanticId property) =>
        container.GetValueOrDefault(property) is SemanticArrayValue array ? array.Values : [];

    static int IndexOf(ImmutableArray<SemanticValue> elements, ElementStep step)
    {
        for (var index = 0; index < elements.Length; index++)
        {
            if (elements[index] is SemanticCompositeValue element &&
                element.Properties.Any(_ => _.TargetProperty == step.IdentifiedBy && SemanticValueRules.AreEqual(_.Value, step.Identity)))
            {
                return index;
            }
        }

        return -1;
    }

    static Dictionary<SemanticId, SemanticValue> State(SemanticValue value) =>
        value is SemanticCompositeValue composite ? composite.Properties.ToDictionary(_ => _.TargetProperty, _ => _.Value) : [];

    static SemanticValue Composite(Dictionary<SemanticId, SemanticValue> state) =>
        SemanticValue.Composite([.. state.Select(_ => new SemanticPropertyValue(_.Key, _.Value))]);

    IEnumerable<Location> Elements(ImmutableArray<ChildStep> address) =>
        _documents
            .Where(_ => !_.Removed)
            .SelectMany(document => Walk(document.State, address, 0, []).Select(steps => new Location(document, steps, [])));

    IEnumerable<ImmutableArray<ElementStep>> Walk(
        Dictionary<SemanticId, SemanticValue> container,
        ImmutableArray<ChildStep> address,
        int index,
        ImmutableArray<ElementStep> prefix)
    {
        if (index == address.Length)
        {
            yield return prefix;
            yield break;
        }

        var step = address[index];
        foreach (var element in Elements(container, step.Property))
        {
            var state = State(element);
            var identity = state.GetValueOrDefault(step.IdentifiedBy, SemanticValue.Null);
            foreach (var steps in Walk(state, address, index + 1, prefix.Add(new(step.Property, step.IdentifiedBy, identity))))
            {
                yield return steps;
            }
        }
    }

    void Modify(Location location, bool create, Action<Dictionary<SemanticId, SemanticValue>> leaf)
    {
        if (ModifyElement(location.Document.State, location.Steps, 0, create, element => ModifyNested(element, location.Nested, 0, create, leaf)))
        {
            location.Document.Touched = true;
        }
    }

    void RemoveElement(Location location)
    {
        var last = location.Steps[^1];
        var removed = ModifyElement(location.Document.State, location.Steps[..^1], 0, false, parent =>
        {
            var elements = Elements(parent, last.Property);
            var position = IndexOf(elements, last);
            if (position >= 0)
            {
                parent[last.Property] = SemanticValue.Array(elements.RemoveAt(position));
            }

            return position >= 0;
        });
        location.Document.Touched |= removed;
    }

    // A missing element is created with its identity already set (Chronicle ExpandoObjectExtensions.EnsurePath).
    bool ModifyElement(
        Dictionary<SemanticId, SemanticValue> container,
        ImmutableArray<ElementStep> steps,
        int index,
        bool create,
        Func<Dictionary<SemanticId, SemanticValue>, bool> leaf)
    {
        if (index == steps.Length)
        {
            return leaf(container);
        }

        var step = steps[index];
        var elements = Elements(container, step.Property);
        var position = IndexOf(elements, step);
        if (position < 0 && !create)
        {
            return false;
        }

        var element = position < 0 ? Seed(_types[_properties[step.Property].Type.Target].Properties) : State(elements[position]);
        element.TryAdd(step.IdentifiedBy, step.Identity);
        if (!ModifyElement(element, steps, index + 1, create, leaf))
        {
            return false;
        }

        var value = Composite(element);
        container[step.Property] = SemanticValue.Array(position < 0 ? elements.Add(value) : elements.SetItem(position, value));
        return true;
    }

    // An absent nested object is created on first touch and merged by later touches (Chronicle ExpandoObjectExtensions.cs:181-186).
    bool ModifyNested(
        Dictionary<SemanticId, SemanticValue> container,
        ImmutableArray<SemanticId> path,
        int index,
        bool create,
        Action<Dictionary<SemanticId, SemanticValue>> leaf)
    {
        if (index == path.Length)
        {
            leaf(container);
            return true;
        }

        var current = container.GetValueOrDefault(path[index]) as SemanticCompositeValue;
        if (current is null && !create)
        {
            return false;
        }

        var nested = current is null ? [] : State(current);
        if (!ModifyNested(nested, path, index + 1, create, leaf))
        {
            return false;
        }

        container[path[index]] = Composite(nested);
        return true;
    }

    void Complete()
    {
        var identifier = _readModel.Properties.Single(_ => _.IsIdentifier);
        foreach (var document in _documents.Where(_ => _.Touched && !_.Removed))
        {
            foreach (var property in _readModel.Properties)
            {
                if (!document.State.TryGetValue(property.Id, out var value))
                {
                    if (!property.Type.IsOptional)
                    {
                        Fail($"Projection '{projection.Name}' did not establish required read-model property '{property.Name}'.");
                        return;
                    }

                    document.State[property.Id] = value = SemanticValue.Null;
                }

                try
                {
                    validator.Validate(value, property.Type, $"read-model property '{property.Name}'");
                }
                catch (InvalidSemanticContract exception)
                {
                    Fail(exception.Message);
                    return;
                }
            }

            if (!SemanticValueRules.AreEqual(document.State[identifier.Id], document.Key))
            {
                Fail($"Projection '{projection.Name}' affected key disagrees with read-model identifier '{identifier.Name}'.");
                return;
            }
        }
    }
}
