// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Builds the JSON Schemas of an OpenAPI document and names its components uniquely.
/// </summary>
sealed class OpenApiSchemas
{
    /// <summary>
    /// The component names the document reserves for the result envelopes.
    /// </summary>
    internal static readonly string[] Reserved = ["CommandResult", "QueryResult", "ValidationResult", "PagingInfo"];

    readonly SemanticApplication _application;
    readonly Dictionary<SemanticId, string> _names = [];
    readonly HashSet<string> _taken = new(Reserved, StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiSchemas"/> class.
    /// </summary>
    /// <param name="application">The application whose declarations become components.</param>
    internal OpenApiSchemas(SemanticApplication application) => _application = application;

    /// <summary>
    /// Gets the component schemas, in the order they were named.
    /// </summary>
    internal JsonObject Components { get; } = [];

    /// <summary>
    /// Adds the concepts and composite types every operation may refer to.
    /// </summary>
    internal void AddShared()
    {
        // Name every shared declaration first, so a type can refer to one declared after it.
        var concepts = _application.Concepts.Select(concept => (Name: Name(concept.Id, concept.Name, null), Concept: concept)).ToArray();
        var types = _application.Types.Select(type => (Name: Name(type.Id, type.Name, null), Type: type)).ToArray();
        foreach (var (name, concept) in concepts) Components[name] = Concept(concept);
        foreach (var (name, type) in types)
        {
            Components[name] = Object(type.Properties.Select(property => (ApiNames.Camel(property.Name), property.Type)));
        }
    }

    /// <summary>
    /// Adds an object component and returns a reference to it.
    /// </summary>
    /// <param name="id">The identity of the declaration.</param>
    /// <param name="name">The preferred component name.</param>
    /// <param name="slice">The declaring slice, used to qualify a name already taken.</param>
    /// <param name="properties">The members on the wire with their types.</param>
    /// <returns>A reference to the component.</returns>
    internal JsonObject Add(SemanticId id, string name, string slice, IEnumerable<(string Name, SemanticTypeReference Type)> properties)
    {
        if (!_names.ContainsKey(id)) Components[Name(id, name, slice)] = Object(properties);
        return OpenApiTypes.Reference(_names[id]);
    }

    /// <summary>
    /// Builds the schema of a modeled type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The schema.</returns>
    internal JsonObject Type(SemanticTypeReference type)
    {
        var single = type.Kind == SemanticTypeReferenceKind.Primitive ? OpenApiTypes.Primitive(type.Primitive) : Declared(type.Target);
        return type.IsCollection ? new JsonObject { ["type"] = "array", ["items"] = single } : single;
    }

    JsonObject Declared(SemanticId target) => _names.TryGetValue(target, out var name) ? OpenApiTypes.Reference(name) : [];

    JsonObject Object(IEnumerable<(string Name, SemanticTypeReference Type)> properties)
    {
        var members = new JsonObject();
        var required = new JsonArray();
        foreach (var (name, type) in properties)
        {
            members[name] = Type(type);
            if (!type.IsOptional) required.Add(name);
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = members };
        if (required.Count > 0) schema["required"] = required;
        return schema;
    }

    JsonObject Concept(SemanticConcept concept)
    {
        if (concept.Values.IsDefaultOrEmpty) return OpenApiTypes.Primitive(concept.Primitive);
        var numbers = concept.Numbers ?? [.. Enumerable.Range(0, concept.Values.Length)];
        return new()
        {
            ["type"] = "integer",
            ["enum"] = new JsonArray([.. numbers.Select(number => (JsonNode)number)]),
            ["x-enum-varnames"] = new JsonArray([.. concept.Values.Select(value => (JsonNode)value)])
        };
    }

    string Name(SemanticId id, string name, string? slice)
    {
        var preferred = ApiNames.Pascal(name);
        var candidate = preferred;
        if (_taken.Contains(candidate) && slice is not null) candidate = $"{slice}.{preferred}";
        for (var suffix = 2; _taken.Contains(candidate); suffix++) candidate = $"{preferred}{suffix}";
        _taken.Add(candidate);

        _names[id] = candidate;
        return candidate;
    }
}
