// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal sealed class EsmUnknownCollector(uint major, bool allowUnknowns)
{
    readonly ImmutableArray<UnknownEsmMember>.Builder _members = ImmutableArray.CreateBuilder<UnknownEsmMember>();
    readonly Dictionary<SemanticId, int> _ownerCounts = [];
    int _valueBytes;

    internal ImmutableArray<UnknownEsmMember> Collect(JsonElement root)
    {
        var fallback = SemanticId.Parse(root.GetProperty("application").GetProperty("id").GetString()!);
        Visit(root, EsmBaselineShape.Envelope, string.Empty, (fallback, UnknownEsmCategory.Unclassified));
        return _members.ToImmutable();
    }

    void Visit(JsonElement value, EsmBaselineShape shape, string path, (SemanticId Owner, UnknownEsmCategory Category) context)
    {
        if (shape == EsmBaselineShape.Atom) return;
        switch (value.ValueKind)
        {
            case JsonValueKind.Array:
                var index = 0;
                foreach (var element in value.EnumerateArray()) Visit(element, shape, $"{path}/{index++}", context);
                break;
            case JsonValueKind.Object: VisitObject(value, shape, path, context); break;
        }
    }

    void VisitObject(JsonElement value, EsmBaselineShape shape, string path, (SemanticId Owner, UnknownEsmCategory Category) context)
    {
        var owner = context.Owner;
        if (EsmBaselineShapes.IsOwner(shape) && value.TryGetProperty("id", out var identity)) owner = SemanticId.Parse(identity.GetString()!);
        var category = EsmBaselineShapes.Category(shape, value, context.Category);
        var fields = EsmBaselineShapes.Fields(major, shape);
        foreach (var property in value.EnumerateObject())
        {
            var memberPath = $"{path}/{property.Name}";
            if (!fields.ContainsKey(property.Name))
            {
                Add(property, memberPath, owner, category);
            }
            else
            {
                Visit(property.Value, EsmBaselineShapes.Child(major, shape, property.Name, value), memberPath, (owner, category));
            }
        }
    }

    void Add(JsonProperty property, string path, SemanticId owner, UnknownEsmCategory category)
    {
        if (!allowUnknowns) throw new InvalidSemanticContract("Registered ESM pairs cannot contain unknown members.");
        EsmOpaqueJson.ValidateName(property.Name);
        EsmOpaqueJson.ValidateNames(property.Value);
        var count = _ownerCounts.GetValueOrDefault(owner) + 1;
        if (count > 64 || _members.Count >= 1024) throw new InvalidSemanticContract("The ESM exceeds the per-owner or per-document unknown member limit.");
        var raw = Encoding.UTF8.GetBytes(property.Value.GetRawText());
        _valueBytes += raw.Length;
        if (_valueBytes > 1_048_576) throw new InvalidSemanticContract("The ESM exceeds the total unknown value byte limit.");
        _ownerCounts[owner] = count;
        _members.Add(new(path, owner, category, [.. raw]));
    }
}
