// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Semantics.for_GenerationStrategyCatalog;

public class when_exporting_the_strategy_contract : Specification
{
    static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    string _actual;
    string _expected;

    void Establish() => _expected = File.Exists(VectorPath()) ? File.ReadAllText(VectorPath()).TrimEnd() : string.Empty;

    void Because()
    {
        var vector = JsonSerializer.SerializeToNode(new { identifierMinimumBits = GenerationStrategyCatalog.IdentifierMinimumBits, strategies = GenerationStrategyCatalog.All }, _options)!;
        foreach (var option in vector["strategies"]!.AsArray().SelectMany(strategy => strategy!["options"]!.AsArray()).Where(option => option!["kind"]!.GetValue<string>() == nameof(GenerationOptionKind.Int64Range)))
        {
            option!["minimum"] = option["minimum"]!.GetValue<long>().ToString(CultureInfo.InvariantCulture);
            option["maximum"] = option["maximum"]!.GetValue<long>().ToString(CultureInfo.InvariantCulture);
        }
        _actual = vector.ToJsonString(_options);
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_GENERATION_CATALOG") == "1")
        {
            File.WriteAllText(VectorPath(), _actual + "\n");
            _expected = _actual;
        }
    }

    [Fact] void should_hold_the_export_to_the_public_catalog() => _actual.ShouldEqual(_expected);
    [Fact] void should_pin_every_strategy() => GenerationStrategyCatalog.All.Select(strategy => strategy.Name).ShouldEqual(["uuid", "uuidv7", "ulid", "typeid", "nanoid", "bytes", "pattern", "random", "rule", "sequence"]);
    [Fact] void should_pin_builtin_random_bits() => GenerationStrategyCatalog.All.Take(4).Select(strategy => strategy.FixedBits).ShouldEqual([122, 74, 80, 74]);
    [Fact] void should_keep_sequence_reserved() => GenerationStrategyCatalog.Find("sequence")!.Status.ShouldEqual(GenerationStrategyStatus.AwaitingAdmission);
    [Fact] void should_require_a_target_implementation_for_rule() => GenerationStrategyCatalog.Find("rule")!.Executability.ShouldEqual(GenerationExecutability.RequiresImplementation);
    [Fact] void should_keep_rule_source_and_esm_names_distinct() => GenerationStrategyCatalog.Find("rule")!.EsmKind.ShouldEqual("implementation");
    [Fact] void should_keep_unknown_and_case_variant_spellings_unknown() => new[] { "UUID", "unknown", "implementation" }.Select(GenerationStrategyCatalog.Find).ShouldEachConformTo(strategy => strategy is null);
    [Fact] void should_pin_builtin_admission() => GenerationStrategyCatalog.All.Take(6).Select(strategy => strategy.IntroducedIn).ShouldEachConformTo(version => version == SemanticVersion.V15);
    [Fact] void should_pin_phase_three_admission() => GenerationStrategyCatalog.All.Skip(6).Take(3).Select(strategy => strategy.IntroducedIn).ShouldEachConformTo(version => version == SemanticVersion.V21);
    [Fact] void should_pin_the_identifier_minimum() => GenerationStrategyCatalog.IdentifierMinimumBits.ShouldEqual(64);
    [Fact] void should_express_hex_case_as_an_option_condition() => GenerationStrategyCatalog.Find("bytes")!.Options.Single(option => option.Name == "case").Conditions.Single().Values.ToArray().ShouldEqual(["hex", "base32"]);
    [Fact] void should_pin_typeid_prefix_match_semantics() => PortableMatchPattern.Check(GenerationStrategyCatalog.Find("typeid")!.Options.Single().MatchPattern!).Status.ShouldEqual(PortableMatchStatus.Admitted);

    static string VectorPath([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return Path.Combine(root!.FullName, "Source/Screenplay/Compiler/Conformance/generation-strategies.json");
    }
}
