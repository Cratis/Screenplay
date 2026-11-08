// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contracts;

/// <summary>
/// The command and option definitions used by the tool's dispatcher, argument readers and contract.
/// </summary>
public static class CliCommandCatalog
{
    /// <summary>
    /// Gets the scope option.
    /// </summary>
    public static CliOption Scope { get; } = new("--scope", "<Module>[.<Feature>[.<Slice>]]");

    /// <summary>
    /// Gets the completeness selection option.
    /// </summary>
    public static CliOption Checks { get; } = new("--check", "<name>[,<name>]|all");

    /// <summary>
    /// Gets the opt-in open model question gate.
    /// </summary>
    public static CliOption OpenQuestions { get; } = new("--open-questions", "report|fail");

    /// <summary>
    /// Gets the warnings-as-errors option.
    /// </summary>
    public static CliOption WarnAsError { get; } = new("--warnaserror");

    /// <summary>
    /// Gets the color-disable option.
    /// </summary>
    public static CliOption NoColor { get; } = new("--no-color");

    /// <summary>
    /// Gets the specification-filter option.
    /// </summary>
    public static CliOption Filter { get; } = new("--filter", "<specification-address>");

    /// <summary>
    /// Gets the output-format option.
    /// </summary>
    public static CliOption Format { get; } = new("--format", "text|json");

    /// <summary>
    /// Gets the compile-free effective validation gate report option.
    /// </summary>
    public static CliOption PrintGate { get; } = new("--print-gate");

    /// <summary>
    /// Gets the authored validation gate bypass option.
    /// </summary>
    public static CliOption NoGateFile { get; } = new("--no-gate-file");

    /// <summary>
    /// Gets the explicit root-creation option.
    /// </summary>
    public static CliOption CreateRoot { get; } = new("--create-root", "<directory>", ExcludesPositionalArguments: true);

    /// <summary>
    /// Gets the contract-output option.
    /// </summary>
    public static CliOption Output { get; } = new("--output", "<path>");

    /// <summary>
    /// Gets the default model-check command.
    /// </summary>
    public static CliCommand Check { get; } = new(string.Empty, "[<file.play|folder>]", [Scope, Checks, OpenQuestions, WarnAsError, NoColor, Format, PrintGate, NoGateFile]);

    /// <summary>
    /// Gets the reference-evaluator command.
    /// </summary>
    public static CliCommand Test { get; } = new("test", "[<file.play|folder>]", [Filter, Format]);

    /// <summary>
    /// Gets the MCP command, which also accepts rootless startup.
    /// </summary>
    public static CliCommand Mcp { get; } = new("mcp", "[<root-directory>]", [CreateRoot]);

    /// <summary>
    /// Gets the contract-export command.
    /// </summary>
    public static CliCommand Contract { get; } = new("contract", string.Empty, [Output]);

    /// <summary>
    /// Gets the processing-record output format.
    /// </summary>
    public static CliOption ProcessingFormat { get; } = new("--format", "json|markdown|csv");

    /// <summary>
    /// Gets the controller-name input.
    /// </summary>
    public static CliOption ControllerName { get; } = new("--controller-name", "<name>");

    /// <summary>
    /// Gets the controller-contact input.
    /// </summary>
    public static CliOption ControllerContact { get; } = new("--controller-contact", "<contact>");

    /// <summary>
    /// Gets the processing-record report command.
    /// </summary>
    public static CliCommand Report { get; } = new("report", "processing [<file.play|folder>]", [ProcessingFormat, ControllerName, ControllerContact]);

    /// <summary>
    /// Gets the command listing the environment keys a model needs, and which of them are secrets.
    /// </summary>
    public static CliCommand EnvironmentKeys { get; } = new("environment", "[<file.play|folder>]", [Format]);

    /// <summary>Gets the version a migration reads.</summary>
    public static CliOption MigrateFrom { get; } = new("--from", "<version>", Required: true);

    /// <summary>Gets the version a migration ends at.</summary>
    public static CliOption MigrateTo { get; } = new("--to", "<version>");

    /// <summary>Gets the option that prints what a migration would change and writes nothing.</summary>
    public static CliOption DryRun { get; } = new("--dry-run");

    /// <summary>Gets the machine-readable migration report option.</summary>
    public static CliOption MigrationReport { get; } = new("--report", "<file>");

    /// <summary>Gets the command that migrates a model from one Screenplay version to another.</summary>
    public static CliCommand Migrate { get; } = new("migrate", "<file.play|folder>", [MigrateFrom, MigrateTo, DryRun, MigrationReport]);

    /// <summary>Gets the release admission label option.</summary>
    public static CliOption Release { get; } = new("--release", "<label>");

    /// <summary>Gets the strictly read shipped ESM option.</summary>
    public static CliOption FromEsm { get; } = new("--from-esm", "<file>");

    /// <summary>Gets the deliberate release admission command.</summary>
    public static CliCommand BaselineAdmit { get; } = new("baseline admit", "<root>", [Release, FromEsm]);

    /// <summary>Gets the stored contract equality gate.</summary>
    public static CliCommand BaselineCheck { get; } = new("baseline check", "<root>", []);

    /// <summary>Gets the executable model export format option.</summary>
    public static CliOption ExportFormat { get; } = new("--format", "esm");

    /// <summary>Gets the executable model export command.</summary>
    public static CliCommand Export { get; } = new("export", "[<file.play|folder>]", [ExportFormat, Scope, Output]);

    /// <summary>Gets the model comparison command.</summary>
    public static CliCommand Diff { get; } = new("diff", "<before> <after>", [Format]);

    /// <summary>Gets the declaration kind option that disambiguates a rename.</summary>
    public static CliOption Kind { get; } = new("--kind", "<kind>");

    /// <summary>Gets the identity-preserving rename command.</summary>
    public static CliCommand Rename { get; } = new("rename", "<address> <new-name> [<folder>]", [Kind]);

    /// <summary>
    /// Gets the help command.
    /// </summary>
    public static CliCommand Help { get; } = new("help", string.Empty, [], ["--help", "-h"]);

    /// <summary>
    /// Gets the version command.
    /// </summary>
    public static CliCommand Version { get; } = new("version", string.Empty, [], ["--version"]);

    /// <summary>
    /// Gets all dispatched commands.
    /// </summary>
    public static IReadOnlyList<CliCommand> All { get; } = [Check, Test, Mcp, Contract, Report, EnvironmentKeys, Migrate, BaselineAdmit, BaselineCheck, Export, Diff, Rename, Help, Version];

    /// <summary>
    /// Resolves the first argument using the owning definitions.
    /// </summary>
    /// <param name="arguments">The process arguments.</param>
    /// <returns>The selected command, or the default check command.</returns>
    public static CliCommand Resolve(string[] arguments) => All.FirstOrDefault(command => command.Name == arguments.FirstOrDefault() || command.Name == string.Join(' ', arguments.Take(2)) || command.Aliases.Contains(arguments.FirstOrDefault() ?? string.Empty)) ?? Check;
}

/// <summary>
/// A command recognized by the tool dispatcher.
/// </summary>
/// <param name="Name">The dispatch token.</param>
/// <param name="Arguments">The positional-argument syntax.</param>
/// <param name="Options">The recognized options.</param>
/// <param name="InformationAliases">The aliases for an information command.</param>
public sealed record CliCommand(string Name, string Arguments, IReadOnlyList<CliOption> Options, IReadOnlyList<string>? InformationAliases = null)
{
    /// <summary>
    /// Gets the aliases.
    /// </summary>
    public IReadOnlyList<string> Aliases => InformationAliases ?? [];

    /// <summary>
    /// Gets usage generated from this same command definition.
    /// </summary>
    public string Usage => string.Join(' ', new[] { "screenplay", Name, Arguments }.Where(value => value.Length > 0).Concat(Options.Select(Format)));

    static string Format(CliOption option)
    {
        if (option.ExcludesPositionalArguments) return $"| {option.Name} {option.Value}";
        var text = $"{option.Name}{(option.Value is null ? string.Empty : " " + option.Value)}";
        return option.Required ? text : $"[{text}]";
    }
}

/// <summary>
/// An option recognized by a command argument reader.
/// </summary>
/// <param name="Name">The option token.</param>
/// <param name="Value">The optional value syntax.</param>
/// <param name="ExcludesPositionalArguments">Whether this option is an alternative to positional arguments.</param>
/// <param name="Required">Whether the command needs the option.</param>
public sealed record CliOption(string Name, string? Value = null, bool ExcludesPositionalArguments = false, bool Required = false);
