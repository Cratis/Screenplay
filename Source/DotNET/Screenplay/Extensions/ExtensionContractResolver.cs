// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Says how strictly unresolved extension references are reported.
/// </summary>
public enum ExtensionResolutionMode
{
    /// <summary>The compiler and CI: a reference missing from the lock or the cache is an error. Nothing resolves a range.</summary>
    Strict,

    /// <summary>The language server, MCP and other editors: an unresolved reference is a warning saying it is not restored yet.</summary>
    Editor
}

/// <summary>
/// Holds the contracts a model's systems resolved to.
/// </summary>
/// <param name="Contracts">The contracts by the reference the model writes.</param>
/// <param name="Diagnostics">What went wrong resolving them.</param>
public sealed record ExtensionResolution(ImmutableDictionary<string, ExtensionContract> Contracts, ImmutableArray<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the folder that holds each restored contract, by the reference the model writes. A package's own files, such as placeholders, sit beside it.
    /// </summary>
    public ImmutableDictionary<string, string> Folders { get; init; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>
    /// Gets a resolution that found nothing and reported nothing.
    /// </summary>
    public static ExtensionResolution None { get; } = new(ImmutableDictionary<string, ExtensionContract>.Empty, []);

    /// <summary>
    /// Checks each system's settings against the contract of the extension it names.
    /// </summary>
    /// <param name="application">The parsed model.</param>
    /// <returns>The diagnostics for unknown settings and for secrets and literals on the wrong kind of setting.</returns>
    public ImmutableArray<Diagnostic> CheckSettings(ApplicationSyntax application) =>
        SystemConfigurationContracts.Check(application, Contracts.Select(pair => new ExtensionConfigurationContract(
            pair.Key,
            [.. pair.Value.Settings.Select(setting => new ExtensionSettingContract(setting.Name, setting.Exposure == ExtensionSettingExposure.Secret, null, setting.Exposure != ExtensionSettingExposure.Backend))])));

    /// <summary>
    /// Returns the syntax as if the author had declared what each contract offers: the abilities on the system and the triggers,
    /// qualified by the system, after the declared ones. The printed source is not affected.
    /// </summary>
    /// <param name="application">The parsed model.</param>
    /// <returns>The application with the contracts' contents.</returns>
    public ApplicationSyntax Apply(ApplicationSyntax application)
    {
        if (Contracts.IsEmpty) return application;
        var triggers = new List<TriggerSyntax>(application.Triggers ?? []);
        var systems = new List<SystemSyntax>();
        foreach (var system in application.Systems)
        {
            if (system.Extension is null || !Contracts.TryGetValue(system.Extension.Reference, out var contract))
            {
                systems.Add(system);
                continue;
            }

            var at = system.Extension.Location;
            systems.Add(system with { Abilities = SourceRelocator.Relocate<IEnumerable<AbilitySyntax>>(contract.Abilities, at) });
            foreach (var trigger in contract.Triggers)
            {
                triggers.Add(new($"{system.Name}.{trigger.Name}", [.. trigger.Payload.Select(field => new TriggerDataSyntax(field.Name, field.Type is null ? null : SourceRelocator.Relocate(field.Type, at), at))], at, null));
            }
        }

        return application with { Systems = systems, Triggers = triggers };
    }
}

/// <summary>
/// Reads each system's extension contract from the lock beside the model root and the digest-keyed cache.
/// </summary>
/// <remarks>
/// Contracts are read from the vendored <c>extensions</c> folder beside the lock first, then from the digest-keyed cache.
/// </remarks>
/// <param name="cache">The <see cref="ExtensionCache"/> to read from.</param>
/// <param name="mode">How strictly to report unresolved references.</param>
/// <param name="fixedLock">A lock to use instead of looking for one beside the documents, for hosts that hold the lock in memory.</param>
/// <param name="root">The model root folder; the lock is the first <see cref="ExtensionLock.FileName"/> in it or above it. Without a root the lock is searched for from the document paths and then the working directory.</param>
public class ExtensionContractResolver(ExtensionCache cache, ExtensionResolutionMode mode = ExtensionResolutionMode.Strict, ExtensionLock? fixedLock = null, string? root = null)
{
    /// <summary>The name of the vendored folder beside the lock.</summary>
    public const string VendorFolder = "extensions";

    static readonly AsyncLocal<ExtensionContractResolver?> _current = new();

    /// <summary>
    /// Gets or sets the resolver the binder uses. Scoped to the current asynchronous flow so hosts and specifications can
    /// replace it without affecting each other; the default is a strict resolver over <see cref="ExtensionCache.Default"/>.
    /// </summary>
    public static ExtensionContractResolver Current
    {
        get => _current.Value ?? new(ExtensionCache.Default);
        set => _current.Value = value;
    }

    /// <summary>
    /// Finds and parses the lock that belongs to documents on disk by looking for <see cref="ExtensionLock.FileName"/> in each
    /// document's folder and above.
    /// </summary>
    /// <param name="documentPaths">The document paths.</param>
    /// <returns>The lock text and its path, or <see langword="null"/> when there is none.</returns>
    public static string? FindLock(IEnumerable<string> documentPaths)
    {
        foreach (var path in documentPaths)
        {
            if (!Path.IsPathRooted(path)) continue;
            for (var directory = Path.GetDirectoryName(path); directory is not null; directory = Path.GetDirectoryName(directory))
            {
                var candidate = Path.Combine(directory, ExtensionLock.FileName);
                if (File.Exists(candidate)) return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the contracts the model's systems name.
    /// </summary>
    /// <param name="application">The parsed model.</param>
    /// <param name="documentPaths">The paths of the documents the model came from.</param>
    /// <returns>The <see cref="ExtensionResolution"/>.</returns>
    public ExtensionResolution Resolve(ApplicationSyntax application, IEnumerable<string> documentPaths)
    {
        var references = application.Systems.Where(system => system.Extension is not null).Select(system => system.Extension!).DistinctBy(extension => extension.Reference).ToArray();
        if (references.Length == 0) return ExtensionResolution.None;
        var lockPath = fixedLock is not null ? null : FindLock(root is not null ? [Path.Combine(Path.GetFullPath(root), "model.play")] : documentPaths.Append(Path.Combine(Directory.GetCurrentDirectory(), "model.play")));
        var vendorRoot = lockPath is null ? null : Path.GetDirectoryName(lockPath);
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var extensionLock = fixedLock ?? ExtensionLock.Empty;
        ExtensionCache[] caches = vendorRoot is null ? [cache] : [new(Path.Combine(vendorRoot, VendorFolder)), cache];
        if (lockPath is not null)
        {
            var parsed = ExtensionLock.Parse(File.ReadAllText(lockPath), lockPath);
            diagnostics.AddRange(parsed.Diagnostics);
            extensionLock = parsed.Value ?? ExtensionLock.Empty;
        }

        return Resolve(references, extensionLock, diagnostics, caches);
    }

    /// <summary>
    /// Resolves references against a lock that is already parsed.
    /// </summary>
    /// <param name="references">The <c>extension</c> lines.</param>
    /// <param name="extensionLock">The lock.</param>
    /// <param name="diagnostics">The diagnostics collected so far.</param>
    /// <param name="caches">The caches to read, in order; the resolver's own cache when omitted.</param>
    /// <returns>The <see cref="ExtensionResolution"/>.</returns>
    public ExtensionResolution Resolve(IEnumerable<SystemExtensionSyntax> references, ExtensionLock extensionLock, ImmutableArray<Diagnostic>.Builder? diagnostics = null, IReadOnlyList<ExtensionCache>? caches = null)
    {
        caches ??= [cache];
        diagnostics ??= ImmutableArray.CreateBuilder<Diagnostic>();
        var contracts = ImmutableDictionary.CreateBuilder<string, ExtensionContract>(StringComparer.Ordinal);
        var folders = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var reference in references)
        {
            var entry = extensionLock.Find(reference.Reference);
            if (entry is null)
            {
                diagnostics.Add(Unresolved(DiagnosticCodes.ExtensionNotLocked, $"Extension '{reference.Reference}' is not in {ExtensionLock.FileName}; run 'screenplay restore' and commit the lock.", reference.Location));
                continue;
            }

            if (entry.Range != ExtensionLock.RangeOf(reference))
            {
                diagnostics.Add(Unresolved(DiagnosticCodes.ExtensionLockStale, $"{ExtensionLock.FileName} pins '{reference.Reference}' for version '{entry.Range}', but the model asks for '{ExtensionLock.RangeOf(reference)}'; run 'screenplay restore --update'.", reference.Location));
                continue;
            }

            if (caches.Select(candidate => candidate.TryRead(entry.Digest)).FirstOrDefault(found => found is not null) is not { } bytes)
            {
                diagnostics.Add(Unresolved(DiagnosticCodes.ExtensionNotRestored, $"Extension '{reference.Reference}' {entry.Version} is not restored.", reference.Location));
                continue;
            }

            if (ExtensionHashes.Of(bytes) != entry.ContractHash)
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.ExtensionContractHashMismatch, $"The cached contract of '{reference.Reference}' does not match the contract hash in {ExtensionLock.FileName}.", reference.Location));
                continue;
            }

            var parsed = ExtensionContractParser.Parse(ExtensionCache.Decode(bytes), $"{reference.Reference}@{entry.Version}/contract.play");
            if (parsed.Value is null)
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.InvalidExtensionContract, $"The contract of '{reference.Reference}' {entry.Version} is not valid: {parsed.Diagnostics.FirstOrDefault()?.Message}", reference.Location));
                continue;
            }

            contracts[reference.Reference] = parsed.Value;
            if (caches.FirstOrDefault(candidate => candidate.TryRead(entry.Digest) is not null) is { } holder && Path.GetDirectoryName(holder.PathOf(entry.Digest)) is { } folder) folders[reference.Reference] = folder;
        }

        return new(contracts.ToImmutable(), diagnostics.ToImmutable()) { Folders = folders.ToImmutable() };
    }

    Diagnostic Unresolved(string code, string message, SourceLocation location) =>
        mode == ExtensionResolutionMode.Strict ? Diagnostic.Error(code, message, location) : Diagnostic.Warning(code, message, location);
}
