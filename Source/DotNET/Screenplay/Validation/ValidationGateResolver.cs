// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Validation;

/// <summary>
/// Applies command-line, authored-file, then default precedence independently per setting.
/// </summary>
public static class ValidationGateResolver
{
    /// <summary>
    /// Resolves a read result with explicit command-line settings. Callers parse their command-line selections with CompletenessChecks.TryParse.
    /// </summary>
    /// <param name="read">The shared reader's result.</param>
    /// <param name="commandLine">Explicit values only; null settings are omitted.</param>
    /// <param name="noGateFile">Whether to bypass the file, including its refusal.</param>
    /// <returns>The effective gate or a refusal.</returns>
    /// <exception cref="InvalidValidationGateSelection">A host supplied an unparsed selection.</exception>
    public static ValidationGateResolution Resolve(ValidationGateReadResult read, DeclaredValidationGate commandLine, bool noGateFile = false)
    {
        if (!noGateFile && read.Failure is not null) return new(null, read.Failure);
        var file = noGateFile ? null : read.Declaration;
        var selections = Select(commandLine.Checks, file?.Checks, []);
        var checks = new ValidationGateValue<ImmutableArray<string>>(Expand(selections.Value), selections.Source);
        var gate = new EffectiveValidationGate(
            read.ModelRoot,
            read.GateFilePath,
            checks,
            Select(commandLine.WarningsAsErrors, file?.WarningsAsErrors, false),
            SelectText(commandLine.Scope, file?.Scope),
            SelectText(commandLine.OpenQuestions, file?.OpenQuestions));
        return new(gate, null);
    }

    static ValidationGateValue<T> Select<T>(T? commandLine, T? file, T fallback)
        where T : struct
    {
        if (commandLine is { } explicitValue) return new(explicitValue, ValidationGateSource.CommandLine);
        if (file is { } declaredValue) return new(declaredValue, ValidationGateSource.File);
        return new(fallback, ValidationGateSource.Default);
    }

    static ValidationGateValue<string?> SelectText(string? commandLine, string? file)
    {
        if (commandLine is not null) return new(commandLine, ValidationGateSource.CommandLine);
        if (file is not null) return new(file, ValidationGateSource.File);
        return new(null, ValidationGateSource.Default);
    }

    static ImmutableArray<string> Expand(ImmutableArray<string> selections)
    {
        var checks = CompletenessChecks.None;
        foreach (var selection in selections)
        {
            if (!CompletenessChecks.TryParse(selection, out var parsed)) throw new InvalidValidationGateSelection(selection);
            checks = new(checks.Selected.Union(parsed.Selected));
        }

        return [.. checks.Selected.Order().Select(CompletenessChecks.NameOf)];
    }
}
