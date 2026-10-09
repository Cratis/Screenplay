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
    public static CliCommand Check { get; } = new(string.Empty, "[<file.play|folder>]", [Scope, Checks, WarnAsError, NoColor]);

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
    public static IReadOnlyList<CliCommand> All { get; } = [Check, Test, Mcp, Contract, Help, Version];

    /// <summary>
    /// Resolves the first argument using the owning definitions.
    /// </summary>
    /// <param name="arguments">The process arguments.</param>
    /// <returns>The selected command, or the default check command.</returns>
    public static CliCommand Resolve(string[] arguments) => All.FirstOrDefault(command => command.Name == arguments.FirstOrDefault() || command.Aliases.Contains(arguments.FirstOrDefault() ?? string.Empty)) ?? Check;
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
    public string Usage => string.Join(' ', new[] { "screenplay", Name, Arguments }.Where(value => value.Length > 0).Concat(Options.Select(option => option.ExcludesPositionalArguments ? $"| {option.Name} {option.Value}" : $"[{option.Name}{(option.Value is null ? string.Empty : " " + option.Value)}]")));
}

/// <summary>
/// An option recognized by a command argument reader.
/// </summary>
/// <param name="Name">The option token.</param>
/// <param name="Value">The optional value syntax.</param>
/// <param name="ExcludesPositionalArguments">Whether this option is an alternative to positional arguments.</param>
public sealed record CliOption(string Name, string? Value = null, bool ExcludesPositionalArguments = false);
