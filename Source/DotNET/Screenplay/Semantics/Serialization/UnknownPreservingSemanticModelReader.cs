// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Admits canonical ESM bytes within a caller's single-major range, retaining opaque additions.
/// This opt-in surface does not change strict reading or confer producer trust or execution authority.
/// </summary>
public static class UnknownPreservingSemanticModelReader
{
    const int MaximumBytes = 8_388_608;

    /// <summary>
    /// Reads and verifies the complete wire document and its strictly validated known projection.
    /// </summary>
    /// <param name="json">The complete canonical UTF-8 document.</param>
    /// <param name="range">The caller's inclusive single-major admission range.</param>
    /// <returns>An owned immutable read view with wire identity and owner-addressable unknowns.</returns>
    /// <exception cref="InvalidSemanticContract">Admission, limits, known semantics, canonical bytes or the revision are invalid.</exception>
    public static UnknownPreservingSemanticDocument Read(ReadOnlySpan<byte> json, EsmAdmissionRange range)
    {
        if (json.Length > MaximumBytes) throw new InvalidSemanticContract("The ESM exceeds the maximum document size of 8388608 bytes.");
        try
        {
            var bytes = json.ToArray();
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 64 });
            return ReadDocument(document.RootElement, bytes, range);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or OverflowException or ArgumentException or KeyNotFoundException)
        {
            throw new InvalidSemanticContract($"The unknown-preserving ESM document is malformed: {exception.Message}");
        }
    }

    static UnknownPreservingSemanticDocument ReadDocument(JsonElement root, byte[] bytes, EsmAdmissionRange range)
    {
        // Reject duplicate members and non-NFC text before plan §1b's wire-pair checks so an ambiguous envelope is never interpreted.
        EsmOpaqueJson.Validate(root);
        var languageVersion = ReadLanguageVersion(root);
        var semanticVersion = ReadSemanticVersion(root);
        ValidatePair(languageVersion, semanticVersion);
        ValidateEnvelope(root, semanticVersion);
        ValidateRange(range, languageVersion, semanticVersion);
        if (!EsmBaselineShapes.Majors.Contains(semanticVersion.Major)) throw new InvalidSemanticContract("The package has no frozen baseline descriptor for this ESM major.");
        var registeredMinor = EsmSchemaV34Support.SemanticVersions.Where(version => version.Major == semanticVersion.Major).Max(version => version.Minor);
        var revision = SemanticRevision.Parse(root.GetProperty("revision").GetString()!);
        var unknowns = new EsmUnknownCollector(semanticVersion.Major, semanticVersion.Minor > registeredMinor).Collect(root);
        var canonical = new EsmAdmissionCanonicalJson(root, semanticVersion.Major, registeredMinor);
        var projectionRevision = SemanticRevision.Compute(canonical.Write(projection: true, revision: null));
        var model = SemanticModelSerializer.Deserialize(canonical.Write(projection: true, revision: projectionRevision));
        var reconstructed = canonical.Write(projection: false, revision: revision);
        if (!bytes.AsSpan().SequenceEqual(reconstructed)) throw new InvalidSemanticContract("The complete ESM bytes are not canonical.");
        var computed = SemanticRevision.Compute(canonical.Write(projection: false, revision: null));
        if (revision != computed) throw new InvalidSemanticContract("The full ESM semantic revision does not match its canonical bytes.");

        // Retain the independently reconstructed bytes only after exact equality with the input.
        // CopyBytes therefore also exposes the opaque writer's byte-identical re-emission.
        return new(reconstructed, (languageVersion, semanticVersion), revision, model, unknowns);
    }

    static LanguageVersion ReadLanguageVersion(JsonElement root)
    {
        var version = ReadWireVersion(root, "languageVersion");
        return new(version.Major, version.Minor);
    }

    static SemanticVersion ReadSemanticVersion(JsonElement root) => ReadWireVersion(root, "semanticVersion");

    static SemanticVersion ReadWireVersion(JsonElement root, string property)
    {
        var text = root.GetProperty(property).GetString() ?? throw new InvalidSemanticContract("The ESM wire version is malformed.");
        var parts = text.Split('.');
        if (parts.Length != 2) throw new InvalidSemanticContract("The ESM wire version is malformed.");
        if (!uint.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major) || !uint.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            throw new InvalidSemanticContract("The ESM wire version is malformed.");
        }

        var version = new SemanticVersion(major, minor);
        if (major == 0 || version.ToString() != text) throw new InvalidSemanticContract("The ESM wire version is not canonical.");
        return version;
    }

    static void ValidateEnvelope(JsonElement root, SemanticVersion version)
    {
        if (root.GetProperty("schema").GetString() != SemanticModelCanonicalJson.Schema || root.GetProperty("schemaVersion").GetUInt32() != version.Major)
        {
            throw new InvalidSemanticContract("The ESM schema does not match the wire version.");
        }
    }

    static bool IsWithin(EsmAdmissionRange range, uint major, uint minor) => range.Major != 0 && range.MinimumMinor <= range.MaximumMinor && major == range.Major && minor >= range.MinimumMinor && minor <= range.MaximumMinor;

    static void ValidateRange(EsmAdmissionRange range, LanguageVersion language, SemanticVersion semantic)
    {
        if (!IsWithin(range, language.Major, language.Minor) || !IsWithin(range, semantic.Major, semantic.Minor))
        {
            throw new InvalidSemanticContract("The ESM wire versions are outside the caller's supported single-major range.");
        }
    }

    static void ValidatePair(LanguageVersion language, SemanticVersion semantic)
    {
        if (language.Major != semantic.Major || language.Minor != semantic.Minor) throw new InvalidSemanticContract("The ESM language and semantic version pair must match.");
    }
}
