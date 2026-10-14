// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Validation;

namespace Cratis.Screenplay.Tool;

static class ModelCheckGate
{
    internal static ValidationGateResolution Resolve(ModelCheckOptions options)
    {
        var root = ValidationGateRoot.Resolve(options.Target);
        var path = Path.Combine(root, "screenplay.json");
        var read = options.NoGateFile ? new ValidationGateReadResult(root, path, null, null) : ScreenplayValidationGate.Read(root);
        var resolution = ValidationGateResolver.Resolve(read, options.CommandLineGate, options.NoGateFile);
        if (resolution.Gate is { } gate) options.UseGate(gate);
        options.UseResolvedNotesRoot = read.Declaration is not null || options.HasFormat || options.NoGateFile || options.PrintGate;
        return resolution;
    }

    internal static void Print(EffectiveValidationGate gate, TextWriter output)
    {
        output.WriteLine($"Model root: {gate.ModelRoot}");
        output.WriteLine($"Gate file: {gate.GateFilePath}");
        output.WriteLine($"Checks: {string.Join(", ", gate.Checks.Value)} (source: {Source(gate.Checks.Source)})");
        output.WriteLine($"Warnings as errors: {gate.WarningsAsErrors.Value.ToString().ToLowerInvariant()} (source: {Source(gate.WarningsAsErrors.Source)})");
        output.WriteLine($"Scope: {gate.Scope.Value ?? "none"} (source: {Source(gate.Scope.Source)})");
        output.WriteLine($"Open questions: {gate.OpenQuestions.Value ?? "not evaluated"} (source: {Source(gate.OpenQuestions.Source)})");
    }

    static string Source(ValidationGateSource source) => source switch
    {
        ValidationGateSource.CommandLine => "commandLine",
        ValidationGateSource.File => "file",
        _ => "default"
    };
}
