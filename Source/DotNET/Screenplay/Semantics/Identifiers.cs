// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the stable identity of a Screenplay application.
/// </summary>
public readonly record struct ApplicationIdentity
{
    readonly string? _value;

    ApplicationIdentity(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the identity is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Creates a deterministic application identity from a stable application key.
    /// </summary>
    /// <param name="stableKey">The stable application key.</param>
    /// <returns>The deterministic identity.</returns>
    public static ApplicationIdentity Create(string stableKey) => new(IdentityHash.CreateApplication(stableKey));

    /// <summary>
    /// Parses a canonical application identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed identity.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static ApplicationIdentity Parse(string value) => new(IdentityText.Parse(value, IdentityText.ApplicationPrefix, nameof(ApplicationIdentity)));

    /// <summary>
    /// Tries to parse a canonical application identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="identity">The parsed identity when successful.</param>
    /// <returns><c>true</c> when the value is canonical; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out ApplicationIdentity identity)
    {
        var success = IdentityText.TryParse(value, IdentityText.ApplicationPrefix, out var canonical);
        identity = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;
}

/// <summary>
/// Represents the stable identity of a Screenplay source document.
/// </summary>
public readonly record struct DocumentId
{
    readonly string? _value;

    DocumentId(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the identity is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Creates a deterministic provisional document identity from a stable key.
    /// </summary>
    /// <param name="stableKey">A stable key that is not a path.</param>
    /// <returns>The deterministic identity.</returns>
    /// <exception cref="InvalidSemanticContract">The key is empty.</exception>
    public static DocumentId Create(string stableKey) => new(IdentityHash.CreateDocument(stableKey));

    /// <summary>
    /// Parses a canonical document identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed identity.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static DocumentId Parse(string value) => new(IdentityText.Parse(value, IdentityText.DocumentPrefix, nameof(DocumentId)));

    /// <summary>
    /// Tries to parse a canonical document identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="id">The parsed identity when successful.</param>
    /// <returns><c>true</c> when the value is canonical; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out DocumentId id)
    {
        var success = IdentityText.TryParse(value, IdentityText.DocumentPrefix, out var canonical);
        id = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;
}

/// <summary>
/// Represents the stable identity of one semantic artifact.
/// </summary>
public readonly record struct SemanticId
{
    readonly string? _value;

    SemanticId(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the identity is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Creates a deterministic provisional identity for an address.
    /// </summary>
    /// <param name="address">The location-independent semantic address.</param>
    /// <returns>The deterministic identity.</returns>
    public static SemanticId Create(SemanticAddress address)
    {
        if (address is null)
        {
            throw new InvalidSemanticContract("A semantic identity requires an address.");
        }

        return new(IdentityHash.CreateSemantic(address));
    }

    /// <summary>
    /// Parses a canonical semantic identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed identity.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static SemanticId Parse(string value) => new(IdentityText.Parse(value, IdentityText.SemanticPrefix, nameof(SemanticId)));

    /// <summary>
    /// Tries to parse a canonical semantic identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="id">The parsed identity when successful.</param>
    /// <returns><c>true</c> when the value is canonical; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out SemanticId id)
    {
        var success = IdentityText.TryParse(value, IdentityText.SemanticPrefix, out var canonical);
        id = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;
}

/// <summary>
/// Represents the stable identity of a persisted event contract.
/// </summary>
public readonly record struct EventContractId
{
    readonly string? _value;

    EventContractId(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the identity is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Creates the deterministic legacy bootstrap identity for an event declaration.
    /// </summary>
    /// <param name="application">The stable application identity.</param>
    /// <param name="eventName">The exact event declaration name.</param>
    /// <returns>The deterministic event contract identity.</returns>
    public static EventContractId CreateLegacy(ApplicationIdentity application, string eventName) =>
        new(IdentityHash.CreateEvent(application, eventName));

    /// <summary>
    /// Parses a canonical event contract identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed identity.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static EventContractId Parse(string value) => new(IdentityText.Parse(value, IdentityText.EventPrefix, nameof(EventContractId)));

    /// <summary>
    /// Tries to parse a canonical event contract identity.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="id">The parsed identity when successful.</param>
    /// <returns><c>true</c> when the value is canonical; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out EventContractId id)
    {
        var success = IdentityText.TryParse(value, IdentityText.EventPrefix, out var canonical);
        id = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;
}

/// <summary>
/// Represents an immutable revision of an event contract.
/// </summary>
/// <param name="Value">The positive revision number.</param>
public readonly record struct EventContractRevision(uint Value)
{
    /// <summary>
    /// Gets the initial event contract revision.
    /// </summary>
    public static readonly EventContractRevision Initial = new(1);

    /// <summary>
    /// Gets a value indicating whether the revision is valid.
    /// </summary>
    public bool IsValid => Value > 0;
}

/// <summary>
/// Represents the deterministic revision of an executable semantic model.
/// </summary>
public readonly record struct SemanticRevision
{
    const string Prefix = "rev1:";
    readonly string? _value;

    SemanticRevision(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the revision is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Parses a canonical semantic revision.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed revision.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static SemanticRevision Parse(string value) => new(IdentityText.Parse(value, Prefix, nameof(SemanticRevision)));

    /// <summary>
    /// Tries to parse a canonical semantic revision.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="revision">The parsed revision when successful.</param>
    /// <returns><c>true</c> when the value is canonical; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out SemanticRevision revision)
    {
        var success = IdentityText.TryParse(value, Prefix, out var canonical);
        revision = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;

    internal static SemanticRevision Compute(ReadOnlySpan<byte> canonicalBytes) =>
        new(RevisionHash.Create(Prefix, "Cratis.Screenplay.SemanticRevision.v1", canonicalBytes));
}

/// <summary>
/// Represents the deterministic revision of a semantic identity catalog.
/// </summary>
public readonly record struct CatalogRevision
{
    const string Prefix = "catrev1:";
    readonly string? _value;

    CatalogRevision(string value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the revision is set.
    /// </summary>
    public bool IsSet => _value is not null;

    /// <summary>
    /// Parses a canonical catalog revision.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed revision.</returns>
    /// <exception cref="InvalidSemanticContract">The value is malformed.</exception>
    public static CatalogRevision Parse(string value) => new(IdentityText.Parse(value, Prefix, nameof(CatalogRevision)));

    /// <summary>
    /// Tries to parse a canonical catalog revision.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="revision">The parsed revision when successful.</param>
    /// <returns><c>true</c> when the value is canonical; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out CatalogRevision revision)
    {
        var success = IdentityText.TryParse(value, Prefix, out var canonical);
        revision = success ? new(canonical!) : default;
        return success;
    }

    /// <inheritdoc/>
    public override string ToString() => _value ?? string.Empty;

    internal static CatalogRevision Compute(ReadOnlySpan<byte> canonicalBytes) =>
        new(RevisionHash.Create(Prefix, "Cratis.Screenplay.CatalogRevision.v1", canonicalBytes));
}

static class IdentityText
{
    internal const string ApplicationPrefix = "app1:";
    internal const string DocumentPrefix = "doc1:";
    internal const string SemanticPrefix = "sem1:";
    internal const string EventPrefix = "evt1:";
    const int HashLength = 64;

    internal static string ToLowerHex(byte[] bytes) => string.Create(
        bytes.Length * 2,
        bytes,
        static (characters, value) =>
        {
            const string Hex = "0123456789abcdef";
            for (var index = 0; index < value.Length; index++)
            {
                characters[index * 2] = Hex[value[index] >> 4];
                characters[(index * 2) + 1] = Hex[value[index] & 0x0f];
            }
        });

    internal static string Parse(string value, string prefix, string type)
    {
        if (!TryParse(value, prefix, out var canonical))
        {
            throw new InvalidSemanticContract($"'{value}' is not a canonical {type}.");
        }

        return canonical!;
    }

    internal static bool TryParse(string? value, string prefix, out string? canonical)
    {
        canonical = null;
        if (value is null || value.Length != prefix.Length + HashLength || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var character in value.AsSpan(prefix.Length))
        {
            if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
            {
                return false;
            }
        }

        canonical = value;
        return true;
    }
}

static class IdentityHash
{
    const uint Version = 1;
    const string Marker = "Cratis.Screenplay.Identity";

    internal static string CreateApplication(string stableKey)
    {
        if (string.IsNullOrEmpty(stableKey))
        {
            throw new InvalidSemanticContract("An application identity key cannot be empty.");
        }

        var part = SemanticAddressPart.Create(SemanticAddressPartKind.Discriminator, stableKey);
        return Create(IdentityText.ApplicationPrefix, "application", 0, [part]);
    }

    internal static string CreateDocument(string stableKey)
    {
        if (string.IsNullOrEmpty(stableKey))
        {
            throw new InvalidSemanticContract("A document identity key cannot be empty.");
        }

        var part = SemanticAddressPart.Create(SemanticAddressPartKind.Discriminator, stableKey);
        return Create(IdentityText.DocumentPrefix, "document", 0, [part]);
    }

    internal static string CreateSemantic(SemanticAddress address) =>
        Create(IdentityText.SemanticPrefix, "semantic", checked((uint)address.Kind), address.Parts);

    internal static string CreateEvent(ApplicationIdentity application, string eventName)
    {
        if (!application.IsSet || string.IsNullOrEmpty(eventName))
        {
            throw new InvalidSemanticContract("A legacy event identity requires an application identity and event name.");
        }

        var bytes = new ArrayBufferWriter<byte>();
        WriteText(bytes, "Cratis.Screenplay.EventContract.LegacyBootstrap.v1");
        WriteText(bytes, application.ToString());
        WriteText(bytes, eventName.Normalize(NormalizationForm.FormC));
        return IdentityText.EventPrefix + IdentityText.ToLowerHex(SHA256.HashData(bytes.WrittenSpan));
    }

    static string Create(string prefix, string domain, uint kind, ImmutableArray<SemanticAddressPart> parts)
    {
        var bytes = new ArrayBufferWriter<byte>();
        WriteText(bytes, Marker);
        WriteText(bytes, domain);
        WriteUInt32(bytes, Version);
        WriteUInt32(bytes, kind);
        WriteUInt32(bytes, checked((uint)parts.Length));
        foreach (var part in parts)
        {
            WriteUInt32(bytes, checked((uint)part.Kind));
            WriteText(bytes, part.Key.Normalize(NormalizationForm.FormC));
        }

        return prefix + IdentityText.ToLowerHex(SHA256.HashData(bytes.WrittenSpan));
    }

    static void WriteText(ArrayBufferWriter<byte> writer, string value)
    {
        var length = Encoding.UTF8.GetByteCount(value);
        WriteUInt32(writer, checked((uint)length));
        var destination = writer.GetSpan(length);
        var written = Encoding.UTF8.GetBytes(value, destination);
        writer.Advance(written);
    }

    static void WriteUInt32(ArrayBufferWriter<byte> writer, uint value)
    {
        var destination = writer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        writer.Advance(sizeof(uint));
    }
}

static class RevisionHash
{
    internal static string Create(string prefix, string domain, ReadOnlySpan<byte> canonicalBytes)
    {
        var bytes = new ArrayBufferWriter<byte>();
        WriteText(bytes, domain);
        WriteUInt32(bytes, checked((uint)canonicalBytes.Length));
        bytes.Write(canonicalBytes);
        return prefix + IdentityText.ToLowerHex(SHA256.HashData(bytes.WrittenSpan));
    }

    static void WriteText(ArrayBufferWriter<byte> writer, string value)
    {
        var length = Encoding.UTF8.GetByteCount(value);
        WriteUInt32(writer, checked((uint)length));
        var destination = writer.GetSpan(length);
        var written = Encoding.UTF8.GetBytes(value, destination);
        writer.Advance(written);
    }

    static void WriteUInt32(ArrayBufferWriter<byte> writer, uint value)
    {
        var destination = writer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        writer.Advance(sizeof(uint));
    }
}
