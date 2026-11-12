// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool;

/// <summary>
/// Pins the extensions a model references in <c>screenplay.lock</c> and restores their contracts into the cache.
/// </summary>
static class RestoreCommand
{
    /// <summary>
    /// Points the compiler at the lock beside the model the command line names, so every command reads contracts from it.
    /// </summary>
    /// <param name="arguments">The process arguments.</param>
    internal static void UseModelRoot(string[] arguments)
    {
        var target = arguments.Skip(1).FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal) && (Directory.Exists(argument) || File.Exists(argument)));
        if (target is null) return;
        var root = Directory.Exists(target) ? target : Path.GetDirectoryName(Path.GetFullPath(target));
        ExtensionContractResolver.Current = new(ExtensionCache.Default, ExtensionResolutionMode.Strict, root: root);
    }

    internal static int Run(string[] arguments, TextWriter output, TextWriter error)
    {
        string? root = null, source = Environment.GetEnvironmentVariable("SCREENPLAY_EXTENSION_SOURCE");
        bool locked = false, update = false, vendor = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument == CliCommandCatalog.Locked.Name)
            {
                locked = true;
            }
            else if (argument == CliCommandCatalog.Update.Name)
            {
                update = true;
            }
            else if (argument == CliCommandCatalog.Vendor.Name)
            {
                vendor = true;
            }
            else if (argument == CliCommandCatalog.PackageSource.Name)
            {
                if (++index >= arguments.Length)
                {
                    error.WriteLine($"{argument} requires a value.");
                    return 2;
                }

                source = arguments[index];
            }
            else if (root is null && !argument.StartsWith("--", StringComparison.Ordinal))
            {
                root = argument;
            }
            else
            {
                error.WriteLine($"Unexpected argument '{argument}'. Usage: {CliCommandCatalog.Restore.Usage}");
                return 2;
            }
        }

        root = Path.GetFullPath(root ?? Directory.GetCurrentDirectory());
        if (!Directory.Exists(root))
        {
            error.WriteLine($"'{root}' must be an existing model directory.");
            return 2;
        }

        if (locked && update)
        {
            error.WriteLine("--locked never resolves, so it cannot be combined with --update.");
            return 2;
        }

        var parsed = new PlayFileCompiler().CompileFolder(root);
        var syntax = parsed.Result.Value;
        if (syntax is null)
        {
            error.WriteLine($"No readable model beneath '{root}'.");
            foreach (var diagnostic in parsed.Result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            return 2;
        }

        var references = syntax.Systems.Where(system => system.Extension is not null).Select(system => system.Extension!).ToArray();
        var lockPath = Path.Combine(root, ExtensionLock.FileName);
        var current = ExtensionLock.Empty;
        if (File.Exists(lockPath))
        {
            var read = ExtensionLock.Parse(File.ReadAllText(lockPath), lockPath);
            if (read.Value is null || read.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                foreach (var diagnostic in read.Diagnostics) error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
                return 1;
            }

            current = read.Value;
        }

        var missing = references.Where(reference => current.Find(reference.Reference)?.Range != ExtensionLock.RangeOf(reference)).DistinctBy(reference => reference.Reference).ToArray();
        if (locked && missing.Length > 0)
        {
            foreach (var reference in missing) error.WriteLine($"{DiagnosticCodes.ExtensionNotLocked}: '{reference.Reference}' is not pinned for version '{ExtensionLock.RangeOf(reference)}' in {ExtensionLock.FileName}.");
            return 1;
        }

        if (source is null && (missing.Length > 0 || update || current.Entries.Length > 0))
        {
            var cacheOnly = !update && missing.Length == 0 && ExtensionRestorerCovers(current, ExtensionCache.Default);
            if (!cacheOnly)
            {
                error.WriteLine("No package source: pass --source <directory> or set SCREENPLAY_EXTENSION_SOURCE.");
                return 2;
            }
        }

        var restorer = new ExtensionRestorer(ExtensionCache.Default, new DirectoryExtensionPackageSource(source ?? root));
        var failures = new List<Diagnostic>();
        var next = current;
        if (!locked)
        {
            var (resolved, diagnostics) = restorer.Resolve(references, current, update);
            next = resolved;
            failures.AddRange(diagnostics);
        }

        next = new ExtensionLock(next.Entries.Where(entry => references.Any(reference => reference.Reference == entry.Reference)));
        failures.AddRange(restorer.RestoreFromLock(next));
        foreach (var failure in failures) error.WriteLine($"{failure.Code}: {failure.Message}");
        if (failures.Count > 0) return 1;

        if (!locked && next.Format() != current.Format())
        {
            File.WriteAllText(lockPath, next.Format());
            output.WriteLine($"Wrote {lockPath}.");
        }

        if (vendor)
        {
            foreach (var entry in next.Entries)
            {
                var bytes = ExtensionCache.Default.TryRead(entry.Digest)!;
                new ExtensionCache(Path.Combine(root, ExtensionContractResolver.VendorFolder)).Store(entry.Digest, bytes);
            }
        }

        output.WriteLine($"Restored {next.Entries.Length} extension(s).");
        return 0;
    }

    static bool ExtensionRestorerCovers(ExtensionLock extensionLock, ExtensionCache cache) =>
        extensionLock.Entries.All(entry => cache.TryRead(entry.Digest) is { } bytes && ExtensionHashes.Of(bytes) == entry.ContractHash);
}
