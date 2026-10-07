// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax.Specifications;

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

            // New syntax vectors have their own full conformance assertions, not a pre-feature baseline.
            if (parsed.SourceOptions != SourceOptions.Legacy || name.StartsWith("source-stream", StringComparison.Ordinal) || name == "named-rule-intent" || name == "specification-examples") continue;

            // Main added route members with transport defaults. Project only those additive empty defaults
            // out of pre-route fixtures; numeric tokens and every previously modeled byte stay untouched.
            // Invoicing is a living sample. Its intentional v7, policy-negation and example additions
            // have full shared conformance vectors; project those out so legacy bytes stay frozen.
            var legacy = name == "invoicing-sample" || name == "invoicing-editor-sample" ? WithoutSampleAdditions(WithoutSampleExamples(parsed)) : parsed;
            var json = SyntaxJson.Serialize(legacy);
            var text = WithoutRuleIntent(json, json.GetRawText())
                .Replace(",\"eventSources\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"stream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"streamCandidates\":[]", string.Empty, StringComparison.Ordinal);
            var actual = Encoding.UTF8.GetBytes(text);
            var path = Path.Combine(folder, "LegacySyntax", document.GetProperty("name").GetString() + ".json");
            if (initializing && !File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, actual);
            }

            var expected = File.ReadAllBytes(path);
            var difference = expected.Zip(actual).TakeWhile(pair => pair.First == pair.Second).Count();
            Assert.True(expected.SequenceEqual(actual), $"Legacy syntax bytes changed for '{name}' at byte {difference}. Expected: {Encoding.UTF8.GetString(expected.AsSpan(difference, Math.Min(200, expected.Length - difference)))}. Actual: {Encoding.UTF8.GetString(actual.AsSpan(difference, Math.Min(200, actual.Length - difference)))}.");
            count++;
        }

        count.ShouldEqual(16);
        Assert.False(initializing, "Review the new protected bytes and rerun without SCREENPLAY_INITIALIZE_LEGACY_SYNTAX_BYTES. Existing baselines are never overwritten.");
    }

    static ApplicationSyntax WithoutSampleExamples(ApplicationSyntax application)
    {
        var expanded = SpecificationExamples.Expand(application);
        expanded.Diagnostics.ShouldBeEmpty();

        return application with
        {
            Modules = application.Modules.Select(module => module with
            {
                Features = module.Features.Select(feature => feature with
                {
                    Slices = feature.Slices.Select(slice => slice.Name == "RegisterInvoice" ? slice with
                    {
                        Examples = [],
                        Specifications = slice.Specifications.Select(specification => specification.When?.CommandType == "AcmeInvoice" ? specification with
                        {
                            When = expanded.Specifications.Single(item => ReferenceEquals(item.Authored, specification)).Effective.When! with
                            {
                                CommandType = "RegisterInvoice",
                                InlineProperty = null,
                                Values = expanded.Specifications.Single(item => ReferenceEquals(item.Authored, specification)).Effective.When!.Values.OrderBy(value => slice.Commands.Single(command => command.Name == "RegisterInvoice").Properties.Select(property => property.Name).ToList().IndexOf(value.Property))
                            }
                        } : specification)
                    } : slice)
                })
            })
        };
    }

    static ApplicationSyntax WithoutSampleAdditions(ApplicationSyntax application) => application with
    {
        Concepts = application.Concepts.Where(concept => concept.Name != "InvoiceReceiptId"),
        Policies = application.Policies.Where(policy => policy.Name != "IsPerson"),
        Personas = application.Personas.Select(persona => persona with { Policies = persona.Policies.Where(policy => policy != "IsPerson") }),
        Modules = application.Modules.Select(module => module with
        {
            Authorize = module.Authorize is { Requirement: PolicyReferenceSyntax reference } authorize && reference.Name == "IsPerson"
                ? authorize with { Requirement = reference with { Name = "IsAuthenticated" } }
                : module.Authorize,
            Features = module.Features.Select(feature => feature.Name == "InvoiceManagement" ? feature with
            {
                Slices = feature.Slices.Where(slice => slice.Name != "StartInvoiceDraft").Select(slice => slice.Name switch
                {
                    "CancelInvoice" => slice with
                    {
                        Commands = slice.Commands.Select(command => command.Name == "CancelInvoice" ? command with { Response = null } : command),
                        Specifications = slice.Specifications.Select(specification => specification.Name == "CancellingAnInvoiceWithARefund" ? specification with { ThenReturns = null } : specification)
                    },
                    "RegisterInvoice" => slice with
                    {
                        Specifications = slice.Specifications.Where(specification => specification.Name != "RejectingAServiceRegisteringAnInvoice")
                    },
                    _ => slice
                })
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
