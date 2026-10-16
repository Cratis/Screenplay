// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Publishes the target-neutral generation strategy and option contract used by the compiler.
/// Listing a strategy does not admit a declaration or execute generation.
/// </summary>
public static partial class GenerationStrategyCatalog
{
    /// <summary>
    /// The minimum exact random-bit support required of generated identifiers.
    /// </summary>
    public const int IdentifierMinimumBits = 64;

    /// <summary>
    /// Gets all strategies in source vocabulary order, including reserved spellings.
    /// </summary>
    public static ImmutableArray<GenerationStrategyDescriptor> All { get; } =
    [
        BuiltIn("uuid", ["Uuid", "String"], 122, [Prefix(), Case("lower", [TextPrimitive()])]),
        BuiltIn("uuidv7", ["Uuid", "String"], 74, [Prefix(), Case("lower", [TextPrimitive()])]),
        BuiltIn("ulid", ["String"], 80, [Prefix(), Case("upper", [])]),
        BuiltIn(
            "typeid",
            ["String"],
            74,
            [Option("prefix", GenerationOptionPlacement.Argument, GenerationOptionKind.Text, required: true, minimum: 0, maximum: 63, charset: "lowercaseAsciiLettersAndUnderscores", matchPattern: "^([a-z]([a-z_]{0,61}[a-z])?)?$", code: DiagnosticCodes.InvalidTypeIdPrefix)]),
        BuiltIn(
            "nanoid",
            ["String"],
            null,
            [
                Option("size", GenerationOptionPlacement.Argument, GenerationOptionKind.WholeNumber, minimum: 1, maximum: 256, defaultValue: "21", code: DiagnosticCodes.GenerationSizeOutsideRange),
                Option("alphabet", GenerationOptionPlacement.Modifier, GenerationOptionKind.Text, minimum: 2, maximum: 256, defaultValue: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-", charset: "printableAscii", distinct: true, code: DiagnosticCodes.InvalidGenerationAlphabet),
                Prefix()
            ]),
        BuiltIn(
            "bytes",
            ["String"],
            null,
            [
                Option("count", GenerationOptionPlacement.Argument, GenerationOptionKind.WholeNumber, required: true, minimum: 1, maximum: 512, code: DiagnosticCodes.GenerationSizeOutsideRange),
                Option("encoding", GenerationOptionPlacement.Argument, GenerationOptionKind.Choice, required: true, choices: ["base64url", "hex", "base32"]),
                Prefix(),
                Case(null, [new(GenerationOptionConditionKind.OptionIn, "encoding", ["hex", "base32"])])
            ]),
        Phase3(
            "pattern",
            "pattern",
            ["String"],
            GenerationExecutability.Reference,
            [
                Option("pattern", GenerationOptionPlacement.Argument, GenerationOptionKind.Pattern, required: true, minimum: 1, maximum: 256, charset: "printableAscii"),
                Option("checksum", GenerationOptionPlacement.Modifier, GenerationOptionKind.Choice, choices: ["luhn", "mod11", "mod97"])
            ]),
        Phase3("random", "random", ["Int"], GenerationExecutability.Reference, [Option("range", GenerationOptionPlacement.Argument, GenerationOptionKind.Int64Range, required: true, minimum: long.MinValue, maximum: long.MaxValue)]),
        Phase3(
            "rule",
            "implementation",
            ["String"],
            GenerationExecutability.RequiresImplementation,
            [
                Option("rule", GenerationOptionPlacement.Argument, GenerationOptionKind.Name, required: true, matchPattern: "^[A-Za-z_][A-Za-z0-9_]*$"),
                Option("implementation", GenerationOptionPlacement.Body, GenerationOptionKind.Implementation, required: true, code: DiagnosticCodes.UnsupportedSemanticSyntax)
            ]),
        new("sequence", null, [], null, GenerationStrategyStatus.AwaitingAdmission, GenerationExecutability.None, null, [])
    ];

    /// <summary>
    /// Finds a strategy by its exact, case-sensitive source spelling.
    /// </summary>
    /// <param name="name">The source strategy name.</param>
    /// <returns>The descriptor, or null for an unknown spelling.</returns>
    public static GenerationStrategyDescriptor? Find(string name) => All.FirstOrDefault(strategy => strategy.Name == name);

    internal static GenerationStrategyDescriptor? FindEsm(string kind) => All.FirstOrDefault(strategy => strategy.EsmKind == kind);

    internal static GenerationOptionDescriptor OptionFor(string strategy, string option) => Find(strategy)!.Options.First(candidate => candidate.Name == option);

    internal static bool AllowsPrimitive(string strategy, string primitive) => Find(strategy) is { } descriptor &&
        (descriptor.Status == GenerationStrategyStatus.AwaitingAdmission || descriptor.Primitives.Contains(primitive, StringComparer.Ordinal));

    internal static bool AllowsModifier(string strategy, string primitive, string? encoding, string modifier) => Find(strategy)?.Options
        .Any(option => option.Name == modifier && option.Placement == GenerationOptionPlacement.Modifier && option.Conditions.All(condition => condition.Kind switch
        {
            GenerationOptionConditionKind.PrimitiveIs => condition.Values.Contains(primitive, StringComparer.Ordinal),
            GenerationOptionConditionKind.OptionIn when condition.Option == "encoding" => encoding is not null && condition.Values.Contains(encoding, StringComparer.Ordinal),
            _ => false
        })) == true;

    internal static bool InRange(GenerationOptionDescriptor option, long value) =>
        (option.Minimum is null || value >= option.Minimum) && (option.Maximum is null || value <= option.Maximum);

    internal static bool ValidText(GenerationOptionDescriptor option, string value) => InRange(option, value.Length) &&
        (option.Charset != "printableAscii" || value.All(character => character is >= ' ' and <= '~')) &&
        (!option.Distinct || value.Distinct().Count() == value.Length) &&
        (option.MatchPattern is null || SemanticMatchPattern.Create(option.MatchPattern).IsMatch(value));

    static GenerationStrategyDescriptor BuiltIn(string name, ImmutableArray<string> primitives, int? bits, ImmutableArray<GenerationOptionDescriptor> options) =>
        new(name, name, primitives, SemanticVersion.V15, GenerationStrategyStatus.Admitted, GenerationExecutability.Reference, bits, options);

    static GenerationStrategyDescriptor Phase3(string name, string kind, ImmutableArray<string> primitives, GenerationExecutability executability, ImmutableArray<GenerationOptionDescriptor> options) =>
        new(name, kind, primitives, SemanticVersion.V21, GenerationStrategyStatus.Admitted, executability, null, options);

    static GenerationOptionCondition TextPrimitive() => new(GenerationOptionConditionKind.PrimitiveIs, null, ["String"]);

    static GenerationOptionDescriptor Prefix() => Option("prefix", GenerationOptionPlacement.Modifier, GenerationOptionKind.Text, minimum: 0, maximum: 32, charset: "printableAscii", conditions: [TextPrimitive()], code: DiagnosticCodes.InvalidGenerationPrefix);

    static GenerationOptionDescriptor Case(string? defaultValue, ImmutableArray<GenerationOptionCondition> conditions) =>
        Option("case", GenerationOptionPlacement.Modifier, GenerationOptionKind.Choice, choices: ["lower", "upper"], defaultValue: defaultValue, conditions: conditions);

    static GenerationOptionDescriptor Option(
        string name,
        GenerationOptionPlacement placement,
        GenerationOptionKind kind,
        bool required = false,
        long? minimum = null,
        long? maximum = null,
        ImmutableArray<string> choices = default,
        string? defaultValue = null,
        string? charset = null,
        bool distinct = false,
        string? matchPattern = null,
        ImmutableArray<GenerationOptionCondition> conditions = default,
        string code = DiagnosticCodes.InvalidGenerationArgument) =>
        new(name, placement, kind, required, minimum, maximum, choices.IsDefault ? [] : choices, defaultValue, charset, distinct, matchPattern, conditions.IsDefault ? [] : conditions, code);
}
