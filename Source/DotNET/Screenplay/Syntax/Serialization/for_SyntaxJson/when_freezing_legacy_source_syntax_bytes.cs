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

            // New feature vectors have their own full conformance assertions, not a pre-feature baseline.
            // Route and refusal fixtures use Legacy mode so their own admission diagnostics are not masked by #285.
            if (parsed.SourceOptions != SourceOptions.Legacy || name.StartsWith("source-stream", StringComparison.Ordinal) || name == "authoring-metadata" || name == "compliance" || name == "named-rule-intent" || name == "specification-examples" || name == "guarded-actions" || name == "no-events" || name == "declared-dependencies" || name == "reaction-refusals-redelivery" || name == "specification-streams") continue;

            // Main added route members with transport defaults. Project only those additive empty defaults
            // out of pre-route fixtures; numeric tokens and every previously modeled byte stay untouched.
            // Invoicing is a living sample. Its v7, policy-negation, examples, pre-input navigation and
            // guarded actions have full shared vectors; project only those additions out of frozen legacy bytes.
            var legacy = name switch
            {
                "invoicing-sample" or "invoicing-editor-sample" => WithoutGuardedSampleAction(WithoutSampleAdditions(WithoutSampleExamples(parsed))),
                "library-sample" => WithoutLibraryForms(parsed),
                _ => parsed
            };
            if (name == "invoicing-sample" || name == "invoicing-editor-sample")
            {
                // Newly demonstrated classifications have their own conformance vector. Preserve the
                // baseline for every existing concept, rather than rewriting protected historical bytes.
                legacy = legacy with { Concepts = legacy.Concepts.Where(concept => concept.Name != "BillingApiKey" && concept.Name != "MedicalBillingNote" && concept.Name != "FraudConvictionNote") };
            }

            if (name == "invoicing" || name == "invoicing-sample" || name == "invoicing-editor-sample") legacy = WithoutSampleDependencies(legacy, name == "invoicing");

            // Report-only metadata has its own conformance vector. Keep existing event metadata
            // protected, and project only the newly supported owners out of pre-metadata bytes.
            var json = SyntaxJson.Serialize(WithoutNewAuthoringMetadata(legacy));
            var text = WithoutRuleIntent(json, json.GetRawText())
                .Replace(",\"eventSources\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"stream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"noStream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"streamCandidates\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"templates\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"parameters\":[],\"route\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"columnMode\":\"Unspecified\",\"columns\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"icons\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"category\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"templateType\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"exposes\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"outlets\":[]", string.Empty, StringComparison.Ordinal);

            // Only the two living samples author the additive no-event assertion. Its own conformance
            // vector protects it; removing that member here keeps all pre-feature bytes frozen.
            if (name == "invoicing-sample" || name == "invoicing-editor-sample")
            {
                text = text.Replace(",\"thenNoEvents\":true", string.Empty, StringComparison.Ordinal);
            }

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

    static ApplicationSyntax WithoutNewAuthoringMetadata(ApplicationSyntax application) => application with
    {
        UiProfiles = application.UiProfiles?.Select(profile => profile with { Icons = [] }),
        Modules = application.Modules.Select(module => module with
        {
            Documentation = null,
            Features = module.Features.Select(WithoutNewAuthoringMetadata)
        })
    };

    static FeatureSyntax WithoutNewAuthoringMetadata(FeatureSyntax feature) => feature with
    {
        Documentation = null,
        Features = feature.Features.Select(WithoutNewAuthoringMetadata),
        Slices = feature.Slices.Select(slice => slice with
        {
            Documentation = null,
            Commands = slice.Commands.Select(command => command with { Documentation = null }),
            ReadModels = (slice.ReadModels ?? []).Select(readModel => readModel with { Documentation = null }),
            Reactions = slice.Reactions.Select(reaction => reaction with { Documentation = null }),
            Specifications = slice.Specifications.Select(specification => specification with { Description = null })
        })
    };

    // Dependency declarations are covered by the shared vectors. Restore the embedded fixture's
    // former single-feature layout here rather than rewriting its protected pre-dependency bytes.
    static ApplicationSyntax WithoutSampleDependencies(ApplicationSyntax application, bool embedded) => application with
    {
        Modules = application.Modules.Select(module => module with
        {
            Features = module.Features.Where(feature => !embedded || feature.Name != "Payments").Select(feature => feature with
            {
                DependsOn = [],
                Slices = embedded && feature.Name == "InvoiceManagement"
                    ? feature.Slices.Concat(module.Features.Single(candidate => candidate.Name == "Payments").Slices)
                    : feature.Slices
            })
        })
    };

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

            // The samples-policy coverage spec protects TagInvoiceForm's newly restored item population.
            Forms = module.Forms?.Where(form => form.Name != "TagInvoiceForm").Select(form =>
                (form.Name == "RecordPaymentForm" ? form with { Populate = new FormPopulateFromItemSyntax(form.Location) } : form) with
                {
                    ColumnMode = FormColumnMode.Unspecified,
                    Columns = []
                }),
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
            } : feature).Select(feature => feature with
            {
                Slices = feature.Slices.Select(slice => slice with
                {
                    Screens = slice.Screens.Select(screen => screen with { Directives = WithoutSampleInputNavigation(RestoreCollectionsToolbar(screen)) })
                })
            })
        })
    };

    // Current shared vectors protect the pre-input click paths. Reconstruct only the historical
    // action-navigation directives and omit the new draft entry point for the frozen legacy sample.
    static IEnumerable<ScreenDirectiveSyntax> WithoutSampleInputNavigation(IEnumerable<ScreenDirectiveSyntax> directives) =>
        directives.Where(directive => directive is not ScreenSectionSyntax { Name: "startInvoiceDraftInput" or "recordDuePaymentInput" or "recordOverduePaymentInput" or "invoiceLineDetailInput" } and not ScreenToolbarSyntax and not ScreenComponentSyntax).Select(directive => directive switch
        {
            ScreenSectionSyntax section when LegacyInputCommand(section.Name) is { } command =>
                new ScreenActionSyntax(command, section.Name == "registerInvoiceDashboardInput" ? "$strings.invoices.actions.newInvoice" : null, new ScreenNavigateSyntax(command + "Screen", null, section.Location), section.Location),
            ScreenSectionSyntax section => section with { Directives = WithoutSampleInputNavigation(section.Directives) },
            ScreenTableSyntax { Target: "lineItems" } table => table with { RowClick = new ScreenNavigateSyntax("InvoiceLineDetail", "lineNumber", table.Location) },
            ScreenTemplateReferenceSyntax template => template with
            {
                Slots = template.Slots.Select(slot => slot with { Directives = WithoutSampleInputNavigation(slot.Directives) })
            },
            _ => directive
        });

    // Payments now start from the due/overdue rows. Restore only the former toolbar for the
    // frozen baseline; the current vectors protect identity-carrying rows and query population.
    static IEnumerable<ScreenDirectiveSyntax> RestoreCollectionsToolbar(ScreenSyntax screen) => screen.Name == "CollectionsBoard"
        ? screen.Directives.Select(directive => directive is ScreenTemplateReferenceSyntax template ? template with
        {
            Slots = new[]
            {
                new ScreenSlotSyntax("toolbar", [new ScreenActionSyntax("RecordPayment", null, new ScreenNavigateSyntax("RecordPaymentScreen", null, template.Location), template.Location)], template.Location)
            }.Concat(template.Slots)
        } : directive)
        : screen.Directives;

    static string? LegacyInputCommand(string section) => section switch
    {
        "registerInvoiceInput" or "registerInvoiceDashboardInput" => "RegisterInvoice",
        "changeInvoiceStatusInput" => "ChangeInvoiceStatus",
        "recordPaymentInput" => "RecordPayment",
        "processInvoiceBatchInput" => "ProcessInvoiceBatch",
        "archiveOldInvoicesInput" => "ArchiveOldInvoices",
        "cancelInvoiceInput" => "CancelInvoice",
        "tagInvoiceInput" => "TagInvoice",
        "updateBillingContactInput" => "UpdateBillingContact",
        "requestPaymentPlanInput" => "RequestPaymentPlan",
        _ => null
    };

    // Library's discovered command forms are protected by its current shared vector.
    // Keep its pre-completeness bytes frozen without rewriting the historical baseline.
    static ApplicationSyntax WithoutLibraryForms(ApplicationSyntax application) => application with
    {
        Modules = application.Modules.Select(module => module with
        {
            Forms = module.Forms?.Where(form => form.Name is not ("AddBookInput" or "BorrowBookInput" or "ReturnBookInput"))
        })
    };

    static ApplicationSyntax WithoutGuardedSampleAction(ApplicationSyntax application) => application with
    {
        Modules = application.Modules.Select(module => module with
        {
            Features = module.Features.Select(feature => feature.Name == "InvoiceManagement" ? feature with
            {
                Slices = feature.Slices.Select(slice => slice.Name == "CancelInvoice" ? slice with
                {
                    Screens = slice.Screens.Select(screen => screen.Name == "CancelInvoiceScreen" ? screen with
                    {
                        Directives = RestorePlainSampleAction(screen.Directives.Where(directive => directive is not ScreenDataSyntax))
                    } : screen)
                } : slice)
            } : feature)
        })
    };

    static IEnumerable<ScreenDirectiveSyntax> RestorePlainSampleAction(IEnumerable<ScreenDirectiveSyntax> directives) => directives.Select(directive => directive switch
    {
        ScreenGuardedActionSyntax { Label: "$strings.invoices.actions.cancel" } guarded => new ScreenActionSyntax("CancelInvoice", guarded.Label, guarded.Navigate, guarded.Location),
        ScreenSectionSyntax section => section with { Directives = RestorePlainSampleAction(section.Directives) },
        ScreenTemplateReferenceSyntax template => template with
        {
            Slots = template.Slots.Select(slot => slot with { Directives = RestorePlainSampleAction(slot.Directives) })
        },
        _ => directive
    });

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
