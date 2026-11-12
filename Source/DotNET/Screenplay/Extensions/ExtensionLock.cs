// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Represents one pinned extension reference of the lock file.
/// </summary>
/// <param name="Reference">The reference exactly as the model writes it.</param>
/// <param name="Range">The version range the entry was resolved for, <c>latest</c> when the model names none.</param>
/// <param name="Resolved">The full reference the name resolved to, for example an OCI reference.</param>
/// <param name="Version">The resolved version.</param>
/// <param name="Digest">The digest of the resolved package; everything uses it.</param>
/// <param name="ContractHash">The hash of the package's <c>contract.play</c>.</param>
/// <param name="Publisher">The identity of the publisher.</param>
public record ExtensionLockEntry(string Reference, string Range, string Resolved, string Version, string Digest, string ContractHash, string Publisher);

/// <summary>
/// Represents the plain-text <c>screenplay.lock</c> file beside the model root.
/// </summary>
public partial class ExtensionLock
{
    /// <summary>The name of the lock file.</summary>
    public const string FileName = "screenplay.lock";

    /// <summary>The range recorded when a model names no version.</summary>
    public const string LatestRange = "latest";

    /// <summary>
    /// Gets an empty lock.
    /// </summary>
    public static ExtensionLock Empty { get; } = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionLock"/> class.
    /// </summary>
    /// <param name="entries">The pinned entries.</param>
    public ExtensionLock(IEnumerable<ExtensionLockEntry> entries)
    {
        Entries = [.. entries.OrderBy(entry => entry.Reference, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the pinned entries ordered by reference.
    /// </summary>
    public ImmutableArray<ExtensionLockEntry> Entries { get; }

    /// <summary>
    /// Gets the range a model's <c>extension</c> line asks for, normalized for comparison with a lock entry.
    /// </summary>
    /// <param name="extension">The system's extension line.</param>
    /// <returns>The normalized range.</returns>
    public static string RangeOf(SystemExtensionSyntax extension) =>
        extension.Version is null ? LatestRange : extension.Version.Trim('"');

    /// <summary>
    /// Finds the entry for a reference.
    /// </summary>
    /// <param name="reference">The reference as the model writes it.</param>
    /// <returns>The entry, or <see langword="null"/> when the reference is not locked.</returns>
    public ExtensionLockEntry? Find(string reference) => Entries.FirstOrDefault(entry => entry.Reference == reference);

    /// <summary>
    /// Returns this lock with an entry added or replaced.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The new lock.</returns>
    public ExtensionLock With(ExtensionLockEntry entry) => new(Entries.Where(existing => existing.Reference != entry.Reference).Append(entry));

    /// <summary>
    /// Parses lock file text.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <param name="path">The path to attribute diagnostics to.</param>
    /// <returns>The <see cref="CompilationResult{T}"/> holding the lock and its diagnostics.</returns>
    public static CompilationResult<ExtensionLock> Parse(string text, string? path = null)
    {
        var diagnostics = new List<Diagnostic>();
        var entries = new List<ExtensionLockEntry>();
        string? reference = null;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        SourceLocation start = SourceLocation.Start.In(path);
        var versionSeen = false;

        void Flush()
        {
            if (reference is null) return;
            var missing = Required.Where(key => !fields.ContainsKey(key)).ToArray();
            if (missing.Length > 0)
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionLock, $"Lock entry '{reference}' is missing {string.Join(", ", missing)}.", start));
            }
            else
            {
                entries.Add(new(reference, fields["range"], fields["resolved"], fields["version"], fields["digest"], fields["contract"], fields["publisher"]));
            }

            reference = null;
            fields.Clear();
        }

        var number = 0;
        foreach (var raw in text.Split('\n'))
        {
            number++;
            var line = raw.TrimEnd('\r');
            var location = start with { Line = number, Column = 1 };
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            if (trimmed == "lock 1")
            {
                versionSeen = true;
                continue;
            }

            if (ReferenceRegex().Match(line) is { Success: true } header)
            {
                Flush();
                reference = StringLiteral.Unescape(header.Groups[1].Value);
                start = location;
                if (entries.Any(entry => entry.Reference == reference)) diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionLock, $"The lock pins '{reference}' twice.", location));
            }
            else if (reference is not null && FieldRegex().Match(line) is { Success: true } field && Fields.Contains(field.Groups[1].Value))
            {
                var value = field.Groups[2].Value.Trim();
                fields[field.Groups[1].Value] = value.StartsWith('"') ? StringLiteral.Unescape(value[1..^1]) : value;
            }
            else
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionLock, $"Unexpected lock line '{trimmed}'.", location));
            }
        }

        Flush();
        if (!versionSeen) diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionLock, "A lock file starts with 'lock 1'.", SourceLocation.Start.In(path)));
        return new(new ExtensionLock(entries), diagnostics);
    }

    /// <summary>
    /// Writes the lock as text. The output is deterministic so it diffs cleanly.
    /// </summary>
    /// <returns>The file text.</returns>
    public string Format()
    {
        var builder = new StringBuilder();
        builder.Append("# screenplay.lock: pins the extensions this model references. Written by 'screenplay restore'; commit it, do not edit it by hand.\n");
        builder.Append("lock 1\n");
        foreach (var entry in Entries)
        {
            builder.Append('\n').Append("extension \"").Append(StringLiteral.Escape(entry.Reference)).Append("\"\n");
            builder.Append("  range ").Append(Quote(entry.Range)).Append('\n');
            builder.Append("  resolved ").Append(Quote(entry.Resolved)).Append('\n');
            builder.Append("  version ").Append(entry.Version).Append('\n');
            builder.Append("  digest ").Append(entry.Digest).Append('\n');
            builder.Append("  contract ").Append(entry.ContractHash).Append('\n');
            builder.Append("  publisher ").Append(Quote(entry.Publisher)).Append('\n');
        }

        return builder.ToString();
    }

    static string Quote(string value) => $"\"{StringLiteral.Escape(value)}\"";

    static readonly string[] Fields = ["range", "resolved", "version", "digest", "contract", "publisher"];

    static readonly string[] Required = Fields;

    [GeneratedRegex(@"^extension\s+""((?:[^""\\]|\\.)*)""\s*$", RegexOptions.NonBacktracking)]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex(@"^\s+([a-z]+)\s+(\S.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex FieldRegex();
}
