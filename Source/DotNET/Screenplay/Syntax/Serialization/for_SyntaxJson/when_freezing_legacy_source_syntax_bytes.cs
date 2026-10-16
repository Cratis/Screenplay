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
            var parsed = compiler.Parse(File.ReadAllText(Path.Combine(root, document.GetProperty("path").GetString()))).Value;
            var name = document.GetProperty("name").GetString();

            // New feature vectors have their own full conformance assertions, not a pre-feature baseline.
            // Route and refusal fixtures use Legacy mode so their own admission diagnostics are not masked by #285.
            if (name == "command-reads" || name == "reaction-batches" || parsed.SourceOptions != SourceOptions.Legacy || name.StartsWith("source-stream", StringComparison.Ordinal) || name == "identity" || name == "reaction-identity" || name == "reaction-event-source" || name == "declaration-descriptions" || name == "purposes" || name == "event-subject" || name == "authoring-metadata" || name == "compliance" || name == "duplicate-compliance" || name == "named-rule-intent" || name == "described-handler" || name == "specification-examples" || name == "persona-callers" || name == "specification-tables" || name == "guarded-actions" || name == "guarded-interactions" || name == "no-events" || name == "declared-dependencies" || name == "reaction-refusals-redelivery" || name == "specification-streams" || name == "public-events" || name == "public-events-admission" || name == "event-translations" || name == "read-model-keys" || name == "enum-member-numbers" || name == "generation" || name == "generation-rule-bodies" || name == "query-validation" || name == "template-references" || name == "toolbar-buttons" || name == "crm-sample" || name == "ticketing-sample" || name == "inventory-sample" || name == "hr-sample" || name == "projects-sample" || name == "administration-sample" || name == "approvals-sample" || name == "orders-sample") continue;

            // Main added route members with transport defaults. Project only those additive empty defaults
            // out of pre-route fixtures; numeric tokens and every previously modeled byte stay untouched.
            // Invoicing is a living sample. Its v7, policy-negation, examples, pre-input navigation and
            // guarded actions have full shared vectors; project only those additions out of frozen legacy bytes.
            var legacy = name switch
            {
                "invoicing-sample" or "invoicing-editor-sample" => WithoutSampleTextRules(WithoutSampleRoutesAndKeys(WithoutGuardedSampleAction(WithoutSampleAdditions(WithoutSampleExamples(parsed))))),
                "library-sample" => WithoutLibraryForms(parsed),
                _ => parsed
            };

            // Numbered members have a new conformance vector. Strip only the living samples' new pins
            // from the pre-numbering baseline; every old name, value and numeric token stays protected.
            if (name == "library-sample" || name == "invoicing-sample" || name == "invoicing-editor-sample")
            {
                legacy = legacy with { Concepts = legacy.Concepts.Select(concept => concept with { ValueNumbers = null }) };
            }

            if (name == "invoicing-sample" || name == "invoicing-editor-sample")
            {
                // Newly demonstrated classifications have their own conformance vector. Preserve the
                // baseline for every existing concept, rather than rewriting protected historical bytes.
                legacy = legacy with { Identity = null, ReactionProductionMode = null, CommandReadMode = null, Concepts = legacy.Concepts.Where(concept => concept.Name != "BillingApiKey" && concept.Name != "MedicalBillingNote" && concept.Name != "FraudConvictionNote") };
            }

            if (name == "invoicing" || name == "invoicing-sample" || name == "invoicing-editor-sample") legacy = WithoutSampleDependencies(legacy, name == "invoicing");

            // Template categories (PLAY0720) have their own conformance vector; the frozen bytes predate them.
            legacy = WithoutTemplateCategories(legacy);

            // Report-only metadata has its own conformance vector. Keep existing event metadata
            // protected, and project only the newly supported owners out of pre-metadata bytes.
            var json = SyntaxJson.Serialize(WithoutNewAuthoringMetadata(legacy));
            var text = WithoutRuleIntent(json, json.GetRawText())
                .Replace(",\"runsAs\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"eventSources\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"stream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"noStream\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"streamCandidates\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"templates\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"parameters\":[],\"route\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"columnMode\":\"Unspecified\",\"columns\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"generationMode\":\"Unspecified\",\"layout\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"icons\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"category\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"templateType\":null", string.Empty, StringComparison.Ordinal)
                .Replace(",\"exposes\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"outlets\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"sampleDataPacks\":[]", string.Empty, StringComparison.Ordinal)
                .Replace(",\"sampleData\":null", string.Empty, StringComparison.Ordinal)
                .Replace("\"appearance\":null,", string.Empty, StringComparison.Ordinal)
                .Replace(",\"newScreenTemplate\":null", string.Empty, StringComparison.Ordinal)
                .Replace("],\"root\":null,\"theme\"", "],\"theme\"", StringComparison.Ordinal)
                .Replace("],\"root\":null}", "]}", StringComparison.Ordinal);

            // Only the two living samples author the additive no-event assertion. Its own conformance
            // vector protects it; removing that member here keeps all pre-feature bytes frozen.
            if (name == "invoicing-sample" || name == "invoicing-editor-sample")
            {
                text = text.Replace(",\"thenNoEvents\":true", string.Empty, StringComparison.Ordinal)
                    .Replace(",\"isSubject\":true", string.Empty, StringComparison.Ordinal)

                    // The samples now spell the caller as $identity, which has its own parser and printer specs;
                    // project it back to the $context.identity node it means so the pre-root bytes stay frozen.
                    .Replace("{\"kind\":\"IdentityExpressionSyntax\",\"path\":\"userName\"}", "{\"kind\":\"ContextExpressionSyntax\",\"path\":\"identity.userName\"}", StringComparison.Ordinal);
            }

            var actual = Encoding.UTF8.GetBytes(text);
            var path = Path.Combine(folder, "LegacySyntax", document.GetProperty("name").GetString() + ".json");
            if (initializing && !File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
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

    static ApplicationSyntax WithoutTemplateCategories(ApplicationSyntax application) => application with
    {
        Layouts = application.Layouts?.Select(layout => layout with { Category = null }),
        Modules = application.Modules.Select(module => module with
        {
            ScreenTemplates = module.ScreenTemplates.Select(template => template with { Category = null }),
            DialogTemplates = module.DialogTemplates?.Select(template => template with { Category = null })
        })
    };

    static ApplicationSyntax WithoutNewAuthoringMetadata(ApplicationSyntax application) => application with
    {
        Purposes = [],
        Policies = application.Policies.Select(policy => policy with { Description = null }),
        Concepts = application.Concepts.Select(concept => concept with
        {
            Description = null,
            Generation = null,
            Attributes = concept.Attributes.Select(attribute => attribute with { Reason = LegacyReason(concept.Name, attribute.Name, attribute.Reason) })
        }),
        UiProfiles = application.UiProfiles?.Select(profile => profile with { Icons = [] }),
        Layouts = application.Layouts?.Select(layout => layout with { Navigation = [] }),
        Modules = application.Modules.Select(module => module with
        {
            Navigation = [],
            Documentation = null,
            Purposes = [],
            Forms = module.Forms?.Select(form => form with { Description = null }),
            Features = module.Features.Select(WithoutNewAuthoringMetadata)
        })
    };

    // Toolbar buttons have their own conformance vector; project them out of the pre-button bytes.
    static ScreenDirectiveSyntax WithoutToolbarButtons(ScreenDirectiveSyntax directive) => directive is ScreenToolbarSyntax toolbar ? toolbar with { Buttons = [] } : directive;

    // Restore only the living sample's moved processing prose; current conformance covers its new text.
    static string? LegacyReason(string concept, string marker, string? reason) => (concept, marker, reason) switch
    {
        ("PersonName", "pii", "The name identifies the person to contact about an invoice.") => "The name of the person to contact about an invoice. Needed to reach them about billing; lawful basis: contract performance. Lives under the customer's subject and is erasable with their key.",
        ("BankAccount", "pii", "A refund payout account identifies sole-proprietor customers.") => "Refund payout account. Remits money back to the customer; lawful basis: contract performance / legal obligation. Personal only for sole-proprietor customers.",
        ("EmailAddress", "pii", "The billing contact address identifies the customer's contact.") => "Billing contact address. Delivers invoices and payment reminders; lawful basis: contract performance. Under the customer's subject, erasable with their key.",
        _ => reason
    };

    static FeatureSyntax WithoutNewAuthoringMetadata(FeatureSyntax feature) => feature with
    {
        Documentation = null,
        Purposes = [],
        Features = feature.Features.Select(WithoutNewAuthoringMetadata),
        Slices = feature.Slices.Select(slice => slice with
        {
            Documentation = null,
            Purposes = [],
            Constraints = slice.Constraints.Select(constraint => constraint with { Description = null }),
            Projections = slice.Projections.Select(projection => projection with { Description = null }),
            Screens = slice.Screens.Select(screen => screen with { Description = null, Directives = screen.Directives.Select(WithoutToolbarButtons) }),
            Commands = slice.Commands.Select(command => command with { Documentation = null }),
            ReadModels = (slice.ReadModels ?? []).Select(readModel => readModel with { Documentation = null }),
            Reactions = slice.Reactions.Select(reaction => reaction with { Documentation = null, RunsAs = null }),
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

    // Only the living Invoicing samples reach this projection. Dedicated text-rule specs protect
    // their new rules; every prior rule, requirement, message and severity stays frozen here.
    static ApplicationSyntax WithoutSampleTextRules(ApplicationSyntax application) => application with
    {
        Concepts = application.Concepts.Select(concept => concept with
        {
            Validations = concept.Validations?.SelectMany(WithoutNewTextRuleBlock)
        })
    };

    static IEnumerable<ValidateSyntax> WithoutNewTextRuleBlock(ValidateSyntax validation)
    {
        if (validation is not DeclarativeValidateSyntax block || !block.Rules.Any(rule => IsTextRule(rule.Rule))) return [validation];
        var retained = block.Rules.Where(rule => !IsTextRule(rule.Rule)).ToArray();

        return retained.Length == 0 && !(block.Requirements ?? []).Any()
            ? []
            : [block with { Rules = retained }];
    }

    static bool IsTextRule(ValidationRuleKind kind) => kind is
        ValidationRuleKind.Contains or ValidationRuleKind.NotContains or ValidationRuleKind.StartsWith or ValidationRuleKind.EndsWith;

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
                            When = expanded.Specifications.Single(item => ReferenceEquals(item.Authored, specification)).Effective.When with
                            {
                                CommandType = "RegisterInvoice",
                                InlineProperty = null,
                                Values = expanded.Specifications.Single(item => ReferenceEquals(item.Authored, specification)).Effective.When.Values.OrderBy(value => slice.Commands.Single(command => command.Name == "RegisterInvoice").Properties.Select(property => property.Name).ToList().IndexOf(value.Property))
                            }
                        } : specification)
                    } : slice)
                })
            })
        };
    }

    // Current shared vectors cover these named additions. Restore only their pre-feature
    // counterparts in the living sample; every other serialized member remains protected.
    static ApplicationSyntax WithoutSampleRoutesAndKeys(ApplicationSyntax application) => application with
    {
        EventSources = application.EventSources.Where(source => source.Name != "Payment"),
        Modules = application.Modules.Select(module => module.Name == "Invoicing" ? module with
        {
            Features = module.Features.Select(WithoutSampleRoutesAndKeys)
        } : module)
    };

    static FeatureSyntax WithoutSampleRoutesAndKeys(FeatureSyntax feature) => feature with
    {
        Features = feature.Features.Select(WithoutSampleRoutesAndKeys),
        Slices = feature.Slices.Select(WithoutSampleRecoverySpecifications).Select(slice => slice.Name switch
        {
            "InvoiceList" => slice with
            {
                ReadModels = slice.ReadModels.Select(model => model.Name == "InvoiceListReadModel" ? model with
                {
                    Properties = model.Properties.Select(property => property.Name == "invoiceId" ? property with { IsKey = false } : property)
                } : model)
            },
            "TagInvoice" => slice with
            {
                Specifications = slice.Specifications.Select(specification => specification.Name == "TaggingAnInvoice" ? specification with
                {
                    ThenEvents = specification.ThenEvents.Select(@event => @event.EventType == "InvoiceTagged" ? @event with { NoStream = null } : @event)
                } : specification)
            },
            "RecordPayment" => slice with
            {
                ReadModels = slice.ReadModels.Where(model => model.Name is not ("InvoicePaymentMethodTotals" or "PaymentAuditReadModel")),
                Reducers = slice.Reducers.Where(reducer => reducer.Name != "PaymentAudit"),
                Queries = slice.Queries.Where(query => query.Name != "ListInvoicePaymentMethodTotals"),
                Commands = slice.Commands.Select(command => command.Name == "RecordPayment" ? command with
                {
                    Stream = null,
                    Reads = command.Reads.Where(read => read.ReadModel != "InvoicePaymentMethodTotals"),
                    Produces = command.Produces.Select(production => production.Event == "CashPaymentRecorded" ? production with { Stream = null } : production)
                } : command),
                Specifications = slice.Specifications.Where(specification => specification.Name != "AuditingACashPayment")
                    .Select(specification => specification.Name == "RecordingACardPayment" ? specification with
                    {
                        ThenEvents = specification.ThenEvents.Select(@event => @event.EventType == "PaymentRecorded" ? @event with { Stream = null } : @event)
                    } : specification)
            },
            "ReconcilePayments" => slice with
            {
                Events = slice.Events.Where(@event => @event.Name != "PaymentReconciliationRefused"),
                Reactions = slice.Reactions.Select(reaction => reaction.Name == "PaymentReconciler" ? reaction with
                {
                    From = null,
                    Triggers = reaction.Triggers.Select(trigger => trigger with
                    {
                        Invokes = trigger.Invokes?.Select(invocation => invocation with { OnRefused = [] })
                    })
                } : reaction)
            },
            _ => slice
        })
    };

    // Current shared vectors cover these v17/v18 assertions; keep the pre-feature source bytes frozen.
    static SliceSyntax WithoutSampleRecoverySpecifications(SliceSyntax slice) => slice.Name switch
    {
        "ReconcilePayments" => slice with
        {
            Specifications = slice.Specifications.Where(specification => specification.Name != "IgnoringAZeroValuePayment")
        },
        "ChaseOverdueInvoices" => slice with
        {
            Specifications = slice.Specifications.Where(specification => specification.Name != "RecoveringAnUndeliveredOverdueReminder")
        },
        "ExchangeRates" => slice with
        {
            Specifications = slice.Specifications.Where(specification => specification.Name != "RejectingAnIncompleteCurrencyCode")
        },
        _ => slice
    };

    static ApplicationSyntax WithoutSampleAdditions(ApplicationSyntax application) => application with
    {
        // Sample data has its own parser, printer and compiler specs; the frozen bytes predate it.
        SampleDataPacks = [],

        // The root template, appearance and new screen default have their own parser, printer and compiler specs;
        // the frozen bytes predate them and spell the shell the legacy way, as a layout.
        UiProfiles = application.UiProfiles?.Select(profile => profile with { SampleData = null, Layout = profile.Layout ?? profile.Root?.Name, Root = null, Appearance = null, NewScreenTemplate = null }),
        Exposures = [],
        InstanceContributions = [],

        // Query validation has a current shared vector; preserve CurrencyCode's pre-rule baseline here.
        Concepts = application.Concepts.Where(concept => concept.Name is not ("InvoiceReceiptId" or "InvoiceRef" or "TicketNumber" or "TenantOrderId"))
            .Select(concept => concept.Name == "CurrencyCode" ? concept with { Validations = [] } : concept),
        Policies = application.Policies.Where(policy => policy.Name != "IsPerson"),
        Personas = application.Personas.Where(persona => persona.Name != "InvoiceDraftCreator").Select(persona => persona with { Policies = persona.Policies.Where(policy => policy != "IsPerson") }),
        Modules = application.Modules.Select(module => module with
        {
            Authorize = module.Authorize is { Requirement: PolicyReferenceSyntax reference } authorize && reference.Name == "IsPerson"
                ? authorize with { Requirement = reference with { Name = "IsAuthenticated" } }
                : module.Authorize,

            // Screen composition has its own round-trip and corpus specs; the frozen bytes predate it.
            ScreenTemplates = module.ScreenTemplates.Select(WithoutComposition),
            Contributions = module.Contributions?.Select(WithoutNavigationItem),

            // The samples-policy coverage spec protects TagInvoiceForm's newly restored item population.
            Forms = module.Forms?.Where(form => form.Name != "TagInvoiceForm").Select(form =>
                (form.Name == "RecordPaymentForm" ? form with { Populate = new FormPopulateFromItemSyntax(form.Location) } : form) with
                {
                    ColumnMode = FormColumnMode.Unspecified,
                    GenerationMode = FormGenerationMode.Unspecified,
                    Columns = [],
                    Layout = null
                }),
            Features = module.Features.Select(feature => feature.Name == "InvoiceManagement" ? feature with
            {
                // Shared vectors cover both new slices; retain every pre-feature sample byte here.
                Slices = feature.Slices.Where(slice => slice.Name != "StartInvoiceDraft" && slice.Name != "ApplyLateFee").Select(slice => slice.Name switch
                {
                    "CancelInvoice" => slice with
                    {
                        Commands = slice.Commands.Select(command => command.Name == "CancelInvoice" ? command with { Response = null } : command),
                        Specifications = slice.Specifications.Select(specification => specification.Name == "CancellingAnInvoiceWithARefund" ? specification with { ThenReturns = null } : specification)
                    },
                    "RegisterInvoice" => slice with
                    {
                        // The living sample's continuation response has a current shared syntax vector.
                        // Keep the pre-response sample bytes frozen rather than rewriting its baseline.
                        Commands = slice.Commands.Select(command => command.Name == "RegisterInvoice" ? command with { Response = null } : command),
                        Specifications = slice.Specifications.Where(specification => specification.Name != "RejectingAServiceRegisteringAnInvoice")
                    },
                    _ => slice
                })
            } : feature).Select(WithoutPublicationSlice).Select(WithoutTranslationDirectionAndSplit).Select(feature => feature with
            {
                Contributions = feature.Contributions?.Select(WithoutNavigationItem),
                Slices = feature.Slices.Select(slice => slice with
                {
                    Screens = slice.Screens.Select(screen => screen with { Directives = WithoutSampleInputNavigation(RestoreCollectionsToolbar(screen)), Contributions = [] })
                })
            })
        })
    };

    static ContributionSyntax WithoutNavigationItem(ContributionSyntax contribution) =>
        contribution with { Id = null, Icon = null, Presentation = null, Group = null, Destination = null };

    static ScreenTemplateSyntax WithoutComposition(ScreenTemplateSyntax template) => template with
    {
        DisplayName = null,
        Description = null,
        RestrictsScopes = false,
        Scopes = [],
        Content = [],
        Arrangement = template.Arrangement is { Root: { } root } arrangement ? arrangement with { Root = WithoutComposition(root) } : template.Arrangement
    };

    static ArrangementNodeSyntax WithoutComposition(ArrangementNodeSyntax node) => node switch
    {
        ArrangementContainerSyntax container => container with { Columns = null, Rows = null, Grow = null, Span = null, Children = [.. container.Children.Select(WithoutComposition)] },
        ArrangementSlotSyntax { GrowFactor: not null } slot => slot with { Grow = false, GrowFactor = null },
        _ => node
    };

    // The outbound publication slice has its own v9 conformance vector; omit it, wherever it nests, from the frozen bytes.
    static FeatureSyntax WithoutPublicationSlice(FeatureSyntax feature) => feature with
    {
        Slices = feature.Slices.Where(slice => slice.Name != "InvoiceSentPublication"),
        Features = feature.Features.Select(WithoutPublicationSlice)
    };

    // Explicit translation direction and the capture/reaction split of LegacyInvoiceSync have their own
    // conformance vectors. Drop the direction and fold MarkInvoicePaidFromLegacyPayment back into the
    // single Translate slice the frozen bytes describe, wherever the slices nest.
    static FeatureSyntax WithoutTranslationDirectionAndSplit(FeatureSyntax feature)
    {
        const string legacySync = "LegacyInvoiceSync";
        const string paymentTranslation = "MarkInvoicePaidFromLegacyPayment";
        var translation = feature.Slices.FirstOrDefault(slice => slice.Name == paymentTranslation);
        var slices = feature.Slices.Where(slice => slice.Name != paymentTranslation).Select(slice => string.Equals(slice.Name, "PaymentProviderSync", StringComparison.Ordinal) || string.Equals(slice.Name, legacySync, StringComparison.Ordinal) ? slice with { Direction = null } : slice);
        if (translation is not null)
        {
            slices = slices.Select(slice => slice.Name == legacySync ? RestoreLegacyPaymentTranslation(slice, translation) : slice);
        }

        return feature with
        {
            Slices = slices,
            Features = feature.Features.Select(WithoutTranslationDirectionAndSplit)
        };
    }

    // The payment translation's expectation was once the last step of the capture specification, and the
    // translation spec once sat after it; the reaction sat after the capture.
    static SliceSyntax RestoreLegacyPaymentTranslation(SliceSyntax capture, SliceSyntax translation)
    {
        var translated = translation.Specifications.First(specification => specification.Name == "TranslatingALegacyPayment");
        return capture with
        {
            Reactions = capture.Reactions.Concat(translation.Reactions),
            Specifications = capture.Specifications.Select(specification => specification.Name == "SeeingAnInvoicePaidInTheLegacySystem"
                ? specification with
                {
                    ThenEvents = specification.ThenEvents.Concat(translated.ThenEvents.Select(then => then with { For = specification.ThenEvents.First().For }))
                }
                : specification).Append(translated)
        };
    }

    // Current shared vectors protect the pre-input click paths. Reconstruct only the historical
    // action-navigation directives and omit the new draft entry point for the frozen legacy sample.
    static IEnumerable<ScreenDirectiveSyntax> WithoutSampleInputNavigation(IEnumerable<ScreenDirectiveSyntax> directives) =>
        directives.Where(directive => directive is not ScreenSectionSyntax { Name: "startInvoiceDraftInput" or "recordDuePaymentInput" or "recordOverduePaymentInput" or "invoiceLineDetailInput" } and not ScreenToolbarSyntax and not ScreenComponentSyntax).Select(directive => directive switch
        {
            ScreenSectionSyntax section when LegacyInputCommand(section.Name) is { } command =>
                new ScreenActionSyntax(command, section.Name == "registerInvoiceDashboardInput" ? "$strings.invoices.actions.newInvoice" : null, new ScreenNavigateSyntax(command + "Screen", null, section.Location), section.Location),
            ScreenSectionSyntax section => section with { Directives = WithoutSampleInputNavigation(section.Directives) },
            ScreenTableSyntax { Target: "lineItems" } table => table with { RowClick = new ScreenNavigateSyntax("InvoiceLineDetail", "lineNumber", table.Location) },
            // The living sample now guards editing by draft status; retain the pre-feature plain
            // action list only in frozen bytes. Current vectors and native interaction specs cover it.
            ScreenTableSyntax table => table with
            {
                Behaviors = table.Behaviors.Select(behavior => behavior with
                {
                    Bindings = behavior.Bindings.Select(binding => binding.Alternatives.Any() ? binding with
                    {
                        Actions = binding.Alternatives.First().Actions,
                        Alternatives = [],
                        Otherwise = null
                    } : binding)
                })
            },
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

        return directory.FullName;
    }
}
