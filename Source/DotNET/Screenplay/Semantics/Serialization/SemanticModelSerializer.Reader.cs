// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelSerializer
{
    /// <summary>
    /// Reads strict canonical ESM v1 UTF-8 JSON and verifies its revision.
    /// </summary>
    /// <param name="json">The canonical UTF-8 JSON bytes.</param>
    /// <returns>The verified executable semantic model.</returns>
    /// <exception cref="InvalidSemanticContract">The JSON is malformed, non-canonical, unknown, duplicated, unresolved, or has a revision mismatch.</exception>
    public static ExecutableSemanticModel Deserialize(ReadOnlySpan<byte> json)
    {
        if (json.IsEmpty)
        {
            throw new InvalidSemanticContract("Canonical ESM JSON cannot be empty.");
        }

        try
        {
            var reader = new Utf8JsonReader(json, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 128
            });
            SemanticModelRead.RequiredToken(ref reader, JsonTokenType.StartObject, "ESM root");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string? schema = null;
            uint? schemaVersion = null;
            LanguageVersion? languageVersion = null;
            SemanticVersion? semanticVersion = null;
            SemanticRevision? revision = null;
            SemanticApplication? application = null;
            while (SemanticModelRead.NextProperty(ref reader, seen, "ESM root") is { } property)
            {
                switch (property)
                {
                    case "schema": schema = SemanticModelRead.String(ref reader, property); break;
                    case "schemaVersion": schemaVersion = SemanticModelRead.UInt32(ref reader, property); break;
                    case "languageVersion": languageVersion = LanguageVersion.Parse(SemanticModelRead.String(ref reader, property)); break;
                    case "semanticVersion": semanticVersion = SemanticVersion.Parse(SemanticModelRead.String(ref reader, property)); break;
                    case "revision": revision = SemanticRevision.Parse(SemanticModelRead.String(ref reader, property)); break;
                    case "application": SemanticModelRead.RequiredToken(ref reader, JsonTokenType.StartObject, property); application = SemanticModelRead.Application(ref reader); break;
                    default: throw SemanticModelRead.Unknown(property, "ESM root");
                }
            }

            if (schema != SemanticModelCanonicalJson.Schema || schemaVersion != SemanticModelCanonicalJson.SchemaVersion ||
                languageVersion is null || semanticVersion is null || revision is null || application is null)
            {
                throw new InvalidSemanticContract("The ESM root is missing a required field or uses an unsupported schema.");
            }

            if (reader.Read() || reader.BytesConsumed != json.Length)
            {
                throw new InvalidSemanticContract("Canonical ESM JSON contains trailing data.");
            }

            var model = ExecutableSemanticModel.Create(languageVersion.Value, semanticVersion.Value, application);
            if (model.Revision != revision.Value)
            {
                throw new InvalidSemanticContract($"Semantic revision '{revision}' does not match computed revision '{model.Revision}'.");
            }

            var canonical = SemanticModelCanonicalJson.Serialize(model);
            if (!json.SequenceEqual(canonical))
            {
                throw new InvalidSemanticContract("ESM JSON is valid but not canonical.");
            }

            return model;
        }
        catch (InvalidSemanticContract)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new InvalidSemanticContract($"ESM JSON is malformed: {error.Message}");
        }
    }
}
