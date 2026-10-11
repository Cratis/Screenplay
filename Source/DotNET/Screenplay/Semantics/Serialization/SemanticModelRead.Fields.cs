// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal delegate void FieldReader<TState>(ref Utf8JsonReader reader, TState state, string property);

    internal static SemanticId Identifier(ref Utf8JsonReader reader, string property) =>
        SemanticId.Parse(String(ref reader, property));

    internal static T Nested<T>(ref Utf8JsonReader reader, ValueReader<T> read, string property)
    {
        RequiredToken(ref reader, JsonTokenType.StartObject, property);
        return read(ref reader);
    }

    internal static bool OnlyTrue(ref Utf8JsonReader reader, string property, string owner)
    {
        var value = Boolean(ref reader, property);
        if (!value) throw Malformed(owner, $"{property} may only be true when present");
        return value;
    }

    internal abstract class ReadState(uint schemaVersion)
    {
        public uint SchemaVersion { get; } = schemaVersion;
    }

    internal abstract class IdentifiedReadState(uint schemaVersion) : ReadState(schemaVersion)
    {
        public SemanticId Id { get; set; }

        public string? Name { get; set; }

        public bool HasIdentity => Id.IsSet && Name is not null;

        public static FieldTable<TState> TableFor<TState>(string owner)
            where TState : IdentifiedReadState =>
            new FieldTable<TState>(owner)
                .Add("id", (ref Utf8JsonReader reader, TState state, string property) => state.Id = Identifier(ref reader, property))
                .Add("name", (ref Utf8JsonReader reader, TState state, string property) => state.Name = String(ref reader, property));
    }

    internal sealed class FieldTable<TState>(string owner)
        where TState : ReadState
    {
        readonly Dictionary<string, Field> _fields = new(StringComparer.Ordinal);

        public FieldTable<TState> Add(string name, FieldReader<TState> read) => Add(name, 0, read);

        public FieldTable<TState> Add(string name, uint sinceVersion, FieldReader<TState> read, Func<TState, bool>? when = null)
        {
            _fields.Add(name, new(read, sinceVersion, when));
            return this;
        }

        public FieldTable<TState> AddAll(string[] names, uint sinceVersion, FieldReader<TState> read)
        {
            foreach (var name in names) Add(name, sinceVersion, read);
            return this;
        }

        public void Read(ref Utf8JsonReader reader, TState state, string? ownerOverride = null)
        {
            var name = ownerOverride ?? owner;
            var seen = NewSeen();
            while (NextProperty(ref reader, seen, name) is { } property)
            {
                if (!_fields.TryGetValue(property, out var field) || !field.AppliesTo(state))
                {
                    throw Unknown(property, name);
                }

                field.Read(ref reader, state, property);
            }
        }

        sealed record Field(FieldReader<TState> Read, uint SinceVersion, Func<TState, bool>? When)
        {
            public bool AppliesTo(TState state) => state.SchemaVersion >= SinceVersion && (When?.Invoke(state) ?? true);
        }
    }
}
#pragma warning restore IDE0350
