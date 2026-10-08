// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Contracts;

/// <summary>
/// Produces the versioned language and tool contract without a model, MCP connection or filesystem reads.
/// </summary>
public static partial class ScreenplayContract
{
    static readonly string[] _constructParsers = ["ScreenplayParser", "SliceParser", "ModuleBody", "FeatureBody"];

    /// <summary>
    /// Writes one deterministic schema-version 1 JSON document, followed by a newline.
    /// </summary>
    /// <param name="output">The destination writer.</param>
    /// <exception cref="InvalidScreenplayContract">An owning catalog cannot be classified completely.</exception>
    public static void Write(TextWriter output) => output.Write(Serialize());

    /// <summary>
    /// Creates a deterministic schema-version 1 JSON document.
    /// </summary>
    /// <returns>The contract JSON, followed by a newline.</returns>
    /// <exception cref="InvalidScreenplayContract">An owning catalog cannot be classified completely.</exception>
    public static string Serialize()
    {
        var parsers = ContractSources.All.Where(entry => entry.Key.StartsWith("compiler/Parsing/", StringComparison.Ordinal)).ToArray();
        var preambles = ContractSources.Matches(ContractSources.Get("compiler/Parsing/SourceOptionsParser.cs"), "LineText.FirstWord\\([^)]*\\) == \"([a-z][a-zA-Z]*)\"").ToArray();
        var keywords = parsers.SelectMany(entry => ContractSources.Matches(entry.Value, "case \"([a-z][a-zA-Z]*)\"")).Concat(preambles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var dispatch = _constructParsers.SelectMany(name => ContractSources.Matches(ContractSources.Get($"compiler/Parsing/{name}.cs"), "case \"([a-z][a-zA-Z]*)\"")).Concat(preambles);
        var clauses = new[] { "description", "documentation", "depends", "authorize", "on", "uses" };
        var constructs = dispatch.Except(clauses, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToArray();
        var rootDispatch = ContractSources.Get("compiler/Parsing/ScreenplayParser.cs").Split("default:", StringSplitOptions.None)[0];
        var topLevel = ContractSources.Matches(rootDispatch, "case \"([a-z][a-zA-Z]*)\"").Concat(preambles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var versions = SupportedVersions();
        var tools = new JsonArray();
        foreach (var tool in McpContract.DescribeTools().OrderBy(tool => tool!["name"]!.GetValue<string>(), StringComparer.Ordinal))
        {
            var schema = tool!["inputSchema"]!.AsObject();
            var required = schema["required"]!.AsArray().Select(value => value!.GetValue<string>()).ToArray();
            tools.Add(new JsonObject
            {
                ["name"] = tool["name"]!.DeepClone(),
                ["requiredParameters"] = Strings(required.Order(StringComparer.Ordinal)),
                ["optionalParameters"] = Strings(schema["properties"]!.AsObject().Select(property => property.Key).Except(required, StringComparer.Ordinal).Order(StringComparer.Ordinal)),
                ["inputSchema"] = schema.DeepClone()
            });
        }
        var contract = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["keywords"] = Strings(keywords),
            ["topLevelConstructs"] = Strings(topLevel),
            ["constructs"] = ContractAdmission.Create(constructs, versions),
            ["diagnostics"] = ContractDiagnostics.Create(),
            ["mcpTools"] = tools,
            ["cliCommands"] = CliCommands(),
            ["esmVersions"] = versions
        };

        return contract.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    internal static JsonArray SupportedVersions()
    {
        var result = new JsonArray();
        foreach (var type in typeof(SemanticVersion).Assembly.GetTypes().Where(type => SchemaSupportRegex().IsMatch(type.Name)).OrderBy(type => int.Parse(ContractSources.Matches(type.Name, @"V(\d+)").Single(), System.Globalization.CultureInfo.InvariantCulture)))
        {
            var schema = int.Parse(ContractSources.Matches(type.Name, @"V(\d+)").Single(), System.Globalization.CultureInfo.InvariantCulture);
            var languages = (IEnumerable<LanguageVersion>)type.GetProperty("LanguageVersions")!.GetValue(null)!;
            var semantics = (IEnumerable<SemanticVersion>)type.GetProperty("SemanticVersions")!.GetValue(null)!;
            var pairs = new JsonArray();
            foreach (var language in languages)
            {
                foreach (var semantic in semantics)
                {
                    var supports = type.GetMethod("Supports", [typeof(LanguageVersion), typeof(SemanticVersion)]);
                    var admitted = supports is not null ? (bool)supports.Invoke(null, [language, semantic])! : schema == 1 && language == LanguageVersion.V1 && semantic == SemanticVersion.V1;
                    if (admitted) pairs.Add(new JsonObject { ["languageVersion"] = language.ToString(), ["semanticVersion"] = semantic.ToString() });
                }
            }
            result.Add(new JsonObject { ["schemaVersion"] = schema, ["supportedPairs"] = pairs });
        }

        return result;
    }

    internal static JsonArray CliCommands()
    {
        var usage = ContractSources.Get("cli/CommandLineInformation.cs");
        var lines = CliUsageRegex().Matches(usage);
        var result = new JsonArray();
        foreach (Match line in lines)
        {
            var syntax = line.Groups[1].Value.Trim();
            var first = syntax.Split(' ')[0];
            var name = first.StartsWith('[') ? string.Empty : first;
            var options = ContractSources.Matches(syntax, "(--[a-z][a-z-]*)").Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            result.Add(new JsonObject { ["name"] = name, ["usage"] = $"screenplay {syntax}", ["options"] = Strings(options) });
        }

        // Information aliases are owned by TryPrint, not manually repeated in this contract.
        var commandNames = result.Select(command => command!["name"]!.GetValue<string>()).Where(name => !name.StartsWith('-')).ToHashSet(StringComparer.Ordinal);
        var information = usage.Split("internal static bool TryPrint", StringSplitOptions.None)[1];
        foreach (var branch in information.Split("return true;", StringSplitOptions.None).SkipLast(1))
        {
            var aliases = ContractSources.Matches(branch, "arguments\\[[01]\\] == \"([^\"]+)\"").Except(commandNames, StringComparer.Ordinal).Distinct(StringComparer.Ordinal).ToArray();
            if (aliases.Length == 0) throw new InvalidScreenplayContract("An information-command branch lacks recognizable argument dispatch.");
            var name = aliases.FirstOrDefault(alias => !alias.StartsWith('-')) ?? aliases[0];
            result.Add(new JsonObject { ["name"] = name, ["aliases"] = Strings(aliases.Where(alias => alias != name).Order(StringComparer.Ordinal)) });
        }

        return result;
    }

    static JsonArray Strings(IEnumerable<string> values) => new([.. values.Select(value => (JsonNode?)JsonValue.Create(value))]);

    [GeneratedRegex(@"^EsmSchemaV\d+Support$", RegexOptions.None, 2000)]
    private static partial Regex SchemaSupportRegex();

    [GeneratedRegex(@"(?m)^\s+screenplay ([^\r\n]+)", RegexOptions.None, 2000)]
    private static partial Regex CliUsageRegex();
}
