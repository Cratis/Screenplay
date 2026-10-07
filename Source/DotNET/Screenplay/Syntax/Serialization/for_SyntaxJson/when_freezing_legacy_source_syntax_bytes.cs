// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_freezing_legacy_source_syntax_bytes
{
    [Fact]
    void should_preserve_the_complete_legacy_native_bytes_without_normalizing_values()
    {
        var root = Root();
        var folder = Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")));
        var compiler = new ScreenplayCompiler();
        var initializing = Environment.GetEnvironmentVariable("SCREENPLAY_INITIALIZE_LEGACY_SYNTAX_BYTES") == "1";
        var count = 0;
        foreach (var document in manifest.RootElement.GetProperty("documents").EnumerateArray())
        {
            var parsed = compiler.Parse(File.ReadAllText(Path.Combine(root, document.GetProperty("path").GetString()!))).Value!;
            var name = document.GetProperty("name").GetString()!;

            // New named-rule vectors have their own full conformance assertions, not a pre-intent baseline.
            if (parsed.SourceOptions != SourceOptions.Legacy || name.StartsWith("source-stream", StringComparison.Ordinal) || name == "named-rule-intent" || name == "declared-dependencies") continue;

            // Main added route members with transport defaults. Project only those additive empty defaults
            // out of pre-route fixtures; numeric tokens and every previously modeled byte stay untouched.
            // Invoicing is a living sample. Its intentional v7 additions have full shared conformance
            // vectors; remove only those additions here so the pre-v7 native bytes stay frozen.
            var legacy = name == "invoicing-sample" || name == "invoicing-editor-sample" ? WithoutV7SampleAdditions(parsed) : parsed;
            var json = SyntaxJson.Serialize(legacy);
            var text = WithoutRuleIntent(json, json.GetRawText())
                .Replace(",\"eventSources\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"stream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"noStream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"streamCandidates\":[]", string.Empty, StringComparison.Ordinal);
            var actual = Encoding.UTF8.GetBytes(text);
            var path = Path.Combine(folder, "LegacySyntax", document.GetProperty("name").GetString() + ".json");
            if (initializing && !File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, actual);
            }

            File.ReadAllBytes(path).SequenceEqual(actual).ShouldBeTrue();
            count++;
        }

        count.ShouldEqual(16);
        Assert.False(initializing, "Review the new protected bytes and rerun without SCREENPLAY_INITIALIZE_LEGACY_SYNTAX_BYTES. Existing baselines are never overwritten.");
    }

    static ApplicationSyntax WithoutV7SampleAdditions(ApplicationSyntax application) => application with
    {
        Concepts = application.Concepts.Where(concept => concept.Name != "InvoiceReceiptId"),
        Modules = application.Modules.Select(module => module with
        {
            Features = module.Features.Select(feature => feature.Name == "InvoiceManagement" ? feature with
            {
                Slices = feature.Slices.Where(slice => slice.Name != "StartInvoiceDraft").Select(slice => slice.Name == "CancelInvoice" ? slice with
                {
                    Commands = slice.Commands.Select(command => command.Name == "CancelInvoice" ? command with { Response = null } : command),
                    Specifications = slice.Specifications.Select(specification => specification.Name == "CancellingAnInvoiceWithARefund" ? specification with { ThenReturns = null } : specification)
                } : slice)
            } : feature)
        })
    };

    // The additive rule wrapper is protected by the named-rule corpus. Project it out only for these
    // pre-intent baselines, using raw slices so every numeric token and preexisting member stays untouched.
    static string WithoutRuleIntent(JsonElement node, string text)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray()) text = WithoutRuleIntent(child, text);
        }
        else if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("kind", out var kind) && kind.GetString() == nameof(ValidationRuleSyntax) && node.TryGetProperty("implementation", out var implementation))
            {
                var raw = node.GetRawText();
                text = text.Replace(raw, raw.Replace(",\"implementation\":" + implementation.GetRawText(), string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
            }
            foreach (var member in node.EnumerateObject()) text = WithoutRuleIntent(member.Value, text);
        }

        return text;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
