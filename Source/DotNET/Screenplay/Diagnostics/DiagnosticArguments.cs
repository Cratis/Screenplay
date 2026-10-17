// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Immutable named diagnostic arguments, ordered by name and compared by value.
/// </summary>
/// <param name="values">The named values to copy.</param>
[JsonConverter(typeof(DiagnosticArgumentsJsonConverter))]
public sealed class DiagnosticArguments(IEnumerable<KeyValuePair<string, DiagnosticArgument>> values) : IReadOnlyDictionary<string, DiagnosticArgument>, IEquatable<DiagnosticArguments>
{
    readonly ImmutableSortedDictionary<string, DiagnosticArgument> _values = values.ToImmutableSortedDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets the empty argument map.
    /// </summary>
    public static DiagnosticArguments Empty { get; } = new([]);

    /// <inheritdoc/>
    public int Count => _values.Count;

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _values.Keys;

    /// <inheritdoc/>
    public IEnumerable<DiagnosticArgument> Values => _values.Values;

    /// <inheritdoc/>
    public DiagnosticArgument this[string key] => _values[key];

    /// <summary>
    /// Adds or replaces one named argument without changing this map.
    /// </summary>
    /// <param name="name">The declared name.</param>
    /// <param name="value">The typed value.</param>
    /// <returns>The new map.</returns>
    public DiagnosticArguments With(string name, DiagnosticArgument value) => new(_values.SetItem(name, value));

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, out DiagnosticArgument value) => _values.TryGetValue(key, out value!);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, DiagnosticArgument>> GetEnumerator() => _values.GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(DiagnosticArguments? other) => other is not null && _values.SequenceEqual(other._values);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DiagnosticArguments other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var value in _values) hash.Add(value);
        return hash.ToHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates the map for a refusal that names the issue tracking its admission.
    /// </summary>
    /// <param name="issue">The issue, such as <c>Cratis/Screenplay#600</c>.</param>
    /// <returns>The argument map.</returns>
    internal static DiagnosticArguments ForIssue(string issue) => Empty.With("issue", DiagnosticArgument.Text(issue));

    /// <summary>
    /// Builds one immutable map from named, typed subjects.
    /// </summary>
    internal sealed class Builder
    {
        readonly Dictionary<string, DiagnosticArgument> _values = new(StringComparer.Ordinal);

        /// <summary>
        /// Adds a subject.
        /// </summary>
        /// <param name="name">The declared name.</param>
        /// <param name="value">The typed value.</param>
        /// <returns>This builder.</returns>
        public Builder Add(string name, DiagnosticArgument value)
        {
            _values.Add(name, value);
            return this;
        }

        /// <summary>
        /// Takes an immutable copy of the subjects.
        /// </summary>
        /// <returns>The argument map.</returns>
        public DiagnosticArguments Build() => new(_values);
    }
}

internal sealed class DiagnosticArgumentsJsonConverter : JsonConverter<DiagnosticArguments>
{
    public override DiagnosticArguments Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize<Dictionary<string, DiagnosticArgument>>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, DiagnosticArguments value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (name, argument) in value)
        {
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, argument, options);
        }
        writer.WriteEndObject();
    }
}
