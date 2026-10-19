// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds compatible source syntax to the selected ESM version and reports every non-bound construct explicitly.
/// </summary>
public sealed partial class SemanticModelBinder : ISemanticModelBinder
{
    readonly int? _utcAssessmentYear;

    /// <summary>
    /// Initializes a binder that captures the current UTC year at each bind.
    /// </summary>
    public SemanticModelBinder()
    {
    }

    /// <summary>
    /// Initializes a binder with an explicit generation assessment year.
    /// </summary>
    /// <param name="utcAssessmentYear">The UTC year whose complete 100-year horizon is assessed.</param>
    public SemanticModelBinder(int utcAssessmentYear)
    {
        if (utcAssessmentYear is < 1 or > 9899) throw new InvalidSemanticContract("Generation assessment year must allow the complete 100-year UTC horizon.");
        _utcAssessmentYear = utcAssessmentYear;
    }

    /// <inheritdoc/>
    public CompilationResult<SemanticCompilation> Bind(
        string applicationName,
        ApplicationSyntax syntax,
        SemanticDocumentSet documents)
    {
        var result = BindCore(applicationName, syntax, documents);
        return result with { Diagnostics = DiagnosticSubjects.Attach(syntax, result.Diagnostics) };
    }

    static (LanguageVersion Language, SemanticVersion Semantic)? SelectBoundVersions(SemanticApplication application, SemanticVersion semanticVersion, bool usesStoredEventNames)
    {
        if (SemanticModelValidator.UsesAnonymousPolicies(application)) return (LanguageVersion.V31, SemanticVersion.V31);
        if (SemanticModelValidator.UsesAcmeSecondAdmissions(application)) return (LanguageVersion.V30, SemanticVersion.V30);
        if (SemanticModelValidator.UsesAcmeAdmissions(application)) return (LanguageVersion.V29, SemanticVersion.V29);
        if (SemanticModelValidator.UsesUuidComparisons(application)) return (LanguageVersion.V28, SemanticVersion.V28);
        if (SemanticModelValidator.UsesCompositeReadModelKeys(application)) return (LanguageVersion.V27, SemanticVersion.V27);
        if (SemanticModelValidator.UsesDateComparisons(application)) return (LanguageVersion.V26, SemanticVersion.V26);
        if (application.CommandReadMode is not null) return (LanguageVersion.V25, SemanticVersion.V25);
        if (SemanticModelValidator.UsesTextRules(application)) return (LanguageVersion.V24, SemanticVersion.V24);
        if (SemanticModelValidator.UsesDescribedHandlers(application)) return (LanguageVersion.V23, SemanticVersion.V23);
        if (application.ReactionProductionMode is not null) return (LanguageVersion.V22, SemanticVersion.V22);
        if (SemanticModelValidator.UsesPhase3Generation(application)) return (LanguageVersion.V21, SemanticVersion.V21);
        if (SemanticModelValidator.UsesGeneratedConceptRules(application))
        {
            return (LanguageVersion.V20, SemanticVersion.V20);
        }

        if (SemanticModelValidator.UsesRefusalBranches(application))
        {
            return (LanguageVersion.V19, SemanticVersion.V19);
        }

        if (SemanticModelValidator.UsesQueryRejections(application))
        {
            return (LanguageVersion.V18, SemanticVersion.V18);
        }

        if (SemanticModelValidator.UsesRecoverySpecifications(application))
        {
            return (LanguageVersion.V17, SemanticVersion.V17);
        }

        return usesStoredEventNames ? (LanguageVersion.V16, SemanticVersion.V16) : SelectNumberVersions(application, semanticVersion);
    }

    static (LanguageVersion Language, SemanticVersion Semantic)? SelectNumberVersions(SemanticApplication application, SemanticVersion semanticVersion)
    {
        if (SemanticModelValidator.UsesConceptGeneration(application))
        {
            return (LanguageVersion.V15, SemanticVersion.V15);
        }

        if (SemanticModelValidator.UsesQueryFilters(application))
        {
            return (LanguageVersion.V14, SemanticVersion.V14);
        }

        if (SemanticModelValidator.UsesConceptCompliance(application))
        {
            return (LanguageVersion.V13, SemanticVersion.V13);
        }

        if (SemanticModelValidator.UsesEnumMemberNumbers(application))
        {
            return (LanguageVersion.V12, SemanticVersion.V12);
        }

        // Number lowering is pure. Select v11 from the retained graph with the validator's own
        // value walk, after examples and routed fixtures have reached their final bound shape.
        if (!semanticVersion.IsAtLeast(SemanticVersion.V12) && SemanticModelValidator.UsesExactNumberLiterals(application, semanticVersion))
        {
            return (LanguageVersion.V11, SemanticVersion.V11);
        }

        return null;
    }

    CompilationResult<SemanticCompilation> BindCore(
        string applicationName,
        ApplicationSyntax syntax,
        SemanticDocumentSet documents)
    {
        if (syntax.ReactionProductionMode is { } mode && mode != ReactionProductionMode.Atomic)
        {
            return CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(DiagnosticCodes.InvalidReactionBatchesDeclaration, "Declare 'reaction batches atomic' once at the application root.", syntax.Location)]);
        }

        var numeric = SourceNumericModes.Errors(syntax);
        if (numeric.Count > 0) return CompilationResult<SemanticCompilation>.Failed(numeric);
        if (syntax.SourceOptions.NumericMode == NumericMode.Exact)
        {
            return CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(
                DiagnosticCodes.UnsupportedSemanticSyntax,
                "Exact numeric source mode is not admitted by any supported executable model (ESM) version yet (#285); this source/syntax implementation does not execute exact documents.",
                syntax.Location,
                DiagnosticArguments.ForIssue("Cratis/Screenplay#285"))]);
        }

        var admission = CommandProductionAdmission(syntax, out var usesPublicEvents);
        if (syntax.Identity?.Details.Any() == true)
        {
            var identityAdmission = new IdentityAdmission(syntax);
            identityAdmission.VisitApplication(syntax);
            admission.AddRange(identityAdmission.Diagnostics);
        }

        // Public-event boundaries are checked on the assembled model whenever it uses them, and also when the admission walk
        // already refused something else; a model that uses none of them is not walked again, so programmatically built
        // syntax the binder reports itself is not visited twice.
        if (usesPublicEvents || admission.Count > 0)
        {
            var publicEvents = ParserContext.ForDiagnostics();
            new PublicEventMetadataValidator(publicEvents).VisitApplication(syntax);
            PublicEventUsageValidator.Validate(syntax, publicEvents);
            admission.AddRange(publicEvents.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }

        if (admission.Count > 0)
        {
            return CompilationResult<SemanticCompilation>.Failed(admission);
        }

        var expansion = SpecificationExamples.Expand(syntax);
        if (expansion.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return CompilationResult<SemanticCompilation>.Failed(expansion.Diagnostics);
        }

        syntax = expansion.Application;
        var steps = new Dictionary<SyntaxNode, EffectiveSpecificationStep>(ReferenceEqualityComparer.Instance);
        foreach (var step in expansion.Specifications.SelectMany(specification => specification.Steps)) steps.TryAdd(step.Effective, step);
        var context = new BindingContext(applicationName, syntax, documents, steps, expansion, _utcAssessmentYear ?? DateTimeOffset.UtcNow.Year);
        try
        {
            var application = context.BindApplication();
            var descriptors = SemanticTypedContextCatalog.Create(application, context.ImplementationRequirements, !context.HasErrors);
            if (context.HasErrors)
            {
                return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics) with { ImplementationRequirements = context.ImplementationRequirements, TypedContextDescriptors = descriptors };
            }

            var languageVersion = LanguageVersion.V1;
            var semanticVersion = SemanticVersion.V1;
            if (context.UsesV10 || context.UsesReactionIdentity)
            {
                languageVersion = LanguageVersion.V10;
                semanticVersion = SemanticVersion.V10;
            }
            else if (context.UsesPublicEvents)
            {
                languageVersion = LanguageVersion.V9;
                semanticVersion = SemanticVersion.V9;
            }
            else if (context.UsesEventRoutes)
            {
                languageVersion = LanguageVersion.V8;
                semanticVersion = SemanticVersion.V8;
            }
            else if (context.UsesV7)
            {
                languageVersion = LanguageVersion.V7;
                semanticVersion = SemanticVersion.V7;
            }
            else if (context.UsesV6)
            {
                languageVersion = LanguageVersion.V6;
                semanticVersion = SemanticVersion.V6;
            }
            else if (context.UsesV5)
            {
                languageVersion = LanguageVersion.V5;
                semanticVersion = SemanticVersion.V5;
            }
            else if (context.UsesV4)
            {
                languageVersion = LanguageVersion.V4;
                semanticVersion = SemanticVersion.V4;
            }
            else if (context.UsesV3)
            {
                languageVersion = LanguageVersion.V3;
                semanticVersion = SemanticVersion.V3;
            }
            else if (context.UsesV2)
            {
                languageVersion = LanguageVersion.V2;
                semanticVersion = SemanticVersion.V2;
            }

            if (SelectBoundVersions(application, semanticVersion, context.UsesStoredEventNames) is { } numberVersions)
            {
                languageVersion = numberVersions.Language;
                semanticVersion = numberVersions.Semantic;
                application = BindingContext.PromoteV2Destinations(application);
                descriptors = SemanticTypedContextCatalog.Create(application, context.ImplementationRequirements, true);
            }

            if (context.UsesV30 && !semanticVersion.IsAtLeast(SemanticVersion.V30))
            {
                languageVersion = LanguageVersion.V30;
                semanticVersion = SemanticVersion.V30;
                application = BindingContext.PromoteV2Destinations(application);
                descriptors = SemanticTypedContextCatalog.Create(application, context.ImplementationRequirements, true);
            }

            EsmSchemaV31Support.EnsureSupported(languageVersion, semanticVersion);
            var failures = SemanticModelValidator.Collect(application, semanticVersion, context.UtcAssessmentYear);
            if (!failures.IsEmpty && !semanticVersion.IsAtLeast(SemanticVersion.V29) &&
                SemanticModelValidator.Collect(application, SemanticVersion.V29, context.UtcAssessmentYear).IsEmpty)
            {
                // Only a construct admitted by decision 0098 (a concept converted to another over the same primitive) is refused
                // below ESM v29 and accepted at it.
                languageVersion = LanguageVersion.V29;
                semanticVersion = SemanticVersion.V29;
                failures = [];
            }

            if (!failures.IsEmpty)
            {
                foreach (var failure in failures)
                {
                    context.Error(DiagnosticCodes.InvalidSemanticBinding, failure.Message, context.FailureLocation(failure));
                }

                return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics) with { ImplementationRequirements = context.ImplementationRequirements, TypedContextDescriptors = descriptors };
            }

            var model = ExecutableSemanticModel.CreateValidated(languageVersion, semanticVersion, application);
            var compiledDocuments = documents;
            if (context.UsesV4)
            {
                // New v4 event addresses need their declared revision before compilation verifies identity
                // coherence. Existing persisted revisions are never advanced here: an explicit revision-bound
                // catalog plan is required. Legacy and lone-generation-1 events already resolve to revision 1.
                var index = SemanticCompilationIndex.Create(application, documents.IdentityCatalog.Application);
                var missing = index.Events
                    .Where(entry => entry.Value.Revision.Value > 1 &&
                        !documents.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(entry.Key)))
                    .Select(entry => new EventContractRevisionAdvancement(entry.Key, entry.Value.Revision))
                    .ToImmutableArray();
                if (!missing.IsEmpty)
                {
                    var catalog = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
                        documents.IdentityCatalog,
                        documents.IdentityCatalog.Revision,
                        [.. documents.Documents.Select(value => value.StableKey)],
                        [.. index.Declarations.Keys],
                        [.. index.Events.Keys],
                        missing).Catalog;
                    compiledDocuments = SemanticDocumentSet.Create(documents.Documents, catalog, documents.AttachmentContents);
                }
            }

            var sourceMap = SemanticSourceMap.Create(context.SourceMapEntries, compiledDocuments.Documents);
            var compilation = SemanticCompilation.Create(model, compiledDocuments, sourceMap).WithSpecificationOrigins(context.SpecificationOrigins);
            return new CompilationResult<SemanticCompilation>(compilation, context.Diagnostics)
            {
                ImplementationRequirements = context.ImplementationRequirements,
                TypedContextDescriptors = [.. descriptors.Select(value => value with { ModelRevision = model.Revision })]
            };
        }
        catch (InvalidSemanticContract exception)
        {
            context.Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, syntax.Location);
            return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics) with { ImplementationRequirements = context.ImplementationRequirements };
        }
    }

    sealed class IdentityAdmission(ApplicationSyntax application) : ScreenplaySyntaxWalker
    {
        readonly HashSet<string> _details = (application.Identity?.Details ?? []).Select(detail => detail.Name).ToHashSet(StringComparer.Ordinal);

        internal List<Diagnostic> Diagnostics { get; } = [];

        public override void VisitIdentity(IdentitySyntax syntax)
        {
            // Source expressions here describe metadata, not executable reads.
        }

        public override void VisitIdentityExpression(IdentityExpressionSyntax syntax)
        {
            var detail = syntax.Path.Split('.')[0];
            if (_details.Contains(detail))
            {
                Diagnostics.Add(Diagnostic.Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Identity detail '{detail}' is not executable: the identity block is authoring metadata until admitted (#600).",
                    syntax.Location,
                    DiagnosticArguments.ForIssue("Cratis/Screenplay#600")));
            }
        }
    }

    private sealed partial class BindingContext(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents, IReadOnlyDictionary<SyntaxNode, EffectiveSpecificationStep> steps, EffectiveSpecificationApplication expansion, int utcAssessmentYear)
    {
        readonly List<Diagnostic> _diagnostics = [];
        readonly Dictionary<SemanticId, EffectiveSpecification> _specificationOrigins = [];
        readonly List<SemanticSourceMapEntry> _sourceMapEntries = [];
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _concepts = new(StringComparer.Ordinal);
        readonly Dictionary<EventSyntax, BoundEvent> _eventDeclarations = [];
        readonly Dictionary<string, BoundEvent> _events = new(StringComparer.Ordinal);
        readonly Dictionary<QuerySyntax, SemanticKeyedQuery> _queryDeclarations = [];
        readonly Dictionary<ReadModelSyntax, BoundReadModel> _readModelDeclarations = [];
        readonly Dictionary<string, BoundReadModel> _readModels = new(StringComparer.Ordinal);
        readonly Dictionary<string, string[]> _readModelPaths = new(StringComparer.Ordinal);
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _types = new(StringComparer.Ordinal);
        readonly ApplicationIdentity _applicationIdentity = documents.IdentityCatalog.Application;

        internal IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        internal ImmutableDictionary<SemanticId, EffectiveSpecification> SpecificationOrigins => _specificationOrigins.ToImmutableDictionary();

        internal int UtcAssessmentYear => utcAssessmentYear;

        internal bool HasErrors => _diagnostics.Exists(_ => _.Severity == DiagnosticSeverity.Error);

        internal bool UsesV2 { get; set; }

        internal bool UsesV3 { get; set; }

        internal bool UsesV4 { get; set; }

        internal bool UsesV5 { get; set; }

        internal bool UsesV7 { get; set; }

        internal bool UsesEventRoutes { get; set; }

        internal bool UsesPublicEvents { get; set; }

        internal bool UsesV10 { get; set; }

        internal bool UsesV30 { get; set; }

        internal bool UsesStoredEventNames { get; set; }

        internal ImmutableArray<SemanticSourceMapEntry> SourceMapEntries => [.. _sourceMapEntries];

        internal static SemanticApplication PromoteV2Destinations(SemanticApplication application) =>
            application with { Modules = [.. application.Modules.Select(PromoteV2Destinations)] };

        internal SemanticApplication BindApplication()
        {
            ReportTopLevelDispositions();
            foreach (var purpose in syntax.Purposes) Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Purpose '{purpose.Name}' is report-only processing metadata and adds no executable behavior.", purpose.Location);
            foreach (var reference in PurposeReferences(syntax)) Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Purpose reference '{reference.Name}' is report-only processing metadata.", reference.Location);
            var applicationAddress = SemanticAddress.ForApplication(_applicationIdentity);
            var applicationId = Resolve(applicationAddress, syntax.Location);

            ValidateComplianceAdmission();
            RegisterTypeDeclarations();
            var triggers = RegisterTriggerDeclarations();
            RegisterEventDeclarations();
            RegisterReadModelDeclarations();
            RegisterQueryDeclarations();
            var concepts = syntax.Concepts.Select(BindConcept).ToImmutableArray();
            _boundConcepts = concepts.GroupBy(concept => concept.Id).ToDictionary(group => group.Key, group => group.First());
            var types = (syntax.Types ?? []).Select(BindType).ToImmutableArray();
            var eventSources = BindEventSources(concepts);
            if (syntax.CommandReadMode is not null) RegisterDecisionReadProjections(concepts);
            var modules = AttachAutomation([.. syntax.Modules.Select(BindModule)]);
            var policies = BindPolicies();
            VerifyPersonaCallers(policies);
            ValidateReactionIdentities(modules, policies);
            var application = new SemanticApplication(
                applicationId,
                applicationName,
                concepts,
                types,
                UsesV2 || UsesV3 || UsesV4 || UsesV5 || UsesV6 || UsesV7 || UsesPublicEvents || UsesV10 || !eventSources.IsEmpty ? [.. modules.Select(PromoteV2Destinations)] : modules)
            {
                Policies = policies,
                Triggers = triggers,
                EventSources = eventSources,
                ReactionProductionMode = syntax.ReactionProductionMode is null ? null : SemanticReactionProductionMode.Atomic,
                CommandReadMode = syntax.CommandReadMode is null ? null : SemanticCommandReadMode.Protected
            };
            ValidateExampleAdmission(application);
            application = BindRoutedFixtureSources(application);
            UsesEventRoutes = SemanticEventRouting.Uses(application);
            ReportLegacyDecisionReads();

            return application;
        }

        internal void Error(string code, string message, SourceLocation location, DiagnosticArguments? arguments = null)
        {
            var caseValue = expansion.Specifications.Where(specification => specification.Case is not null).SelectMany(specification => specification.Case!.Values.Select(value => (specification.Case.Name, Value: value))).FirstOrDefault(value => value.Value.Source.Location.Line > 1 && value.Value.Source.Location == location);
            if (caseValue.Value is not null && !message.StartsWith("Case '", StringComparison.Ordinal)) message = $"Case '{caseValue.Name}' parameter '{caseValue.Value.Property}': {message}";
            if (caseValue.Value is not null && (code == DiagnosticCodes.MissingSpecificationProperty || code == DiagnosticCodes.InvalidSpecificationCaseValue))
            {
                arguments = (arguments ?? DiagnosticArguments.Empty).With("case", DiagnosticArgument.Text(caseValue.Name)).With("parameter", DiagnosticArgument.Text(caseValue.Value.Property));
            }
            _diagnostics.Add(Diagnostic.Error(code, message, location, arguments ?? DiagnosticArguments.Empty));
        }

        static SemanticModule PromoteV2Destinations(SemanticModule module) =>
            module with { Features = [.. module.Features.Select(PromoteV2Destinations)] };

        static SemanticFeature PromoteV2Destinations(SemanticFeature feature) => feature with
        {
            Features = [.. feature.Features.Select(PromoteV2Destinations)],
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(command =>
                {
                    var source = command.Produces.Where(produced => produced.Route is null || (produced.Destination is SemanticResolvedExpression expression && command.Properties.Any(property => property.Id == expression.Target && property.IsIdentifier))).Select(produced => produced.Destination)
                        .OfType<SemanticResolvedExpression>().FirstOrDefault();
                    if (command.Destination is not null || source is null ||
                        (command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null)))
                    {
                        return command;
                    }
                    var property = command.Properties.Single(value => value.Id == source.Target);
                    return command with { Destination = new(property.Type, source) };
                })]
            })]
        };

        void Information(string code, string message, SourceLocation location) =>
            _diagnostics.Add(new(DiagnosticSeverity.Information, code, message, location));

        void ReportTopLevelDispositions()
        {
            if (syntax.Domain is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "A domain declaration is authoring metadata and is not part of ESM v1 behavior.", syntax.Domain.Location);
            }

            foreach (var import in syntax.Imports)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Import '{import.QualifiedName}' is not supported by ESM v1 binding.", import.Location);
            }

            var knownPolicies = syntax.Policies.Select(policy => policy.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var persona in syntax.Personas ?? [])
            {
                foreach (var policy in persona.Policies.Where(policy => !knownPolicies.Contains(policy)))
                {
                    Error(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
                }

                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Persona '{persona.Name}' is authoring metadata and is not part of ESM v1 behavior.", persona.Location);
            }

            if (syntax.Authentication is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Authentication providers are realization metadata and are not part of ESM v1 behavior.", syntax.Authentication.Location);
            }

            foreach (var seed in syntax.Seeds ?? [])
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Event seeding is operational metadata and is not part of ESM v1 behavior.", seed.Location);
            }

            foreach (var profile in syntax.UiProfiles ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"UI profile '{profile.Name}' is explicitly deferred from the backend ESM v1 profile.", profile.Location);
            }

            foreach (var pack in syntax.SampleDataPacks)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Sample data pack '{pack.Name}' is design and preview time only and is explicitly deferred from the backend ESM v1 profile.", pack.Location);
            }

            foreach (var theme in syntax.Themes ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Theme '{theme.Name}' is explicitly deferred from the backend ESM v1 profile.", theme.Location);
            }

            foreach (var layout in syntax.Layouts ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Layout '{layout.Name}' is explicitly deferred from the backend ESM v1 profile.", layout.Location);
            }

            // A behavior is UI behavior: the backend profile does not run it, the way it does not run a screen.
            // Deferred is not dropped - the syntax tree, the printer and the UI consumers carry it in full.
            foreach (var behavior in syntax.Behaviors)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Behavior '{behavior.Name}' is explicitly deferred from the backend ESM v1 profile.", behavior.Location);
            }

            foreach (var exposure in syntax.Exposures)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Exposure for '{exposure.Owner}' is explicitly deferred from the backend ESM v1 profile.", exposure.Location);
            }

            foreach (var instance in syntax.InstanceContributions)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Instance contributions for '{instance.Instance}' are explicitly deferred from the backend ESM v1 profile.", instance.Location);
            }
        }

        void RegisterTypeDeclarations()
        {
            foreach (var concept in syntax.Concepts)
            {
                var address = SemanticAddress.ForConcept(_applicationIdentity, concept.Name);
                _concepts[concept.Name] = (address, Resolve(address, concept.Location));
            }

            foreach (var type in syntax.Types ?? [])
            {
                var address = SemanticAddress.ForCompositeType(_applicationIdentity, type.Name);
                _types[type.Name] = (address, Resolve(address, type.Location));
            }
        }

        void RegisterEventDeclarations()
        {
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var sliceAddress = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var group in EventDeclarations.In(slice).GroupBy(@event => @event.Name, StringComparer.Ordinal))
                {
                    var declarations = group.OrderBy(@event => @event.Generation).ToArray();
                    var bound = BindEvent(sliceAddress, declarations);
                    foreach (var declaration in declarations) _eventDeclarations.Add(declaration, bound);
                    if (!_events.TryAdd(group.Key, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Event reference '{group.Key}' is ambiguous across slices in the current ESM v1 binder.", declarations[^1].Location);
                    }
                }
            }
        }

        void RegisterReadModelDeclarations()
        {
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var sliceAddress = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var readModel in slice.ReadModels ?? [])
                {
                    var bound = BindReadModel(sliceAddress, slice, readModel);
                    _readModelDeclarations.Add(readModel, bound);
                    _readModelPaths.TryAdd(readModel.Name, [.. sliceAddress.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key), readModel.Name]);
                    if (!_readModels.TryAdd(readModel.Name, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Read model reference '{readModel.Name}' is ambiguous across slices in the current ESM v1 binder.", readModel.Location);
                    }
                }
            }
        }

        void RegisterQueryDeclarations()
        {
            foreach (var (module, featurePath, slice) in AllSlices())
            {
                var sliceAddress = SemanticAddress.ForSlice(_applicationIdentity, module, featurePath, slice.Name);
                foreach (var query in slice.Queries)
                {
                    var bound = BindQuery(sliceAddress, query);
                    if (bound is null)
                    {
                        continue;
                    }

                    _queryDeclarations.Add(query, bound with
                    {
                        Authorization = EffectiveAuthorization(
                            query.Authorize,
                            SemanticQueryArguments.KeyProperties(bound),
                            module,
                            featurePath,
                            queryFilterNames: query.Filters.Select(filter => filter.Name))
                    });
                }
            }
        }

        IEnumerable<(string Module, ImmutableArray<string> FeaturePath, SliceSyntax Slice)> AllSlices()
        {
            foreach (var module in syntax.Modules)
            {
                foreach (var value in AllSlices(module.Name, [], module.Features))
                {
                    yield return value;
                }
            }
        }

        IEnumerable<(string Module, ImmutableArray<string> FeaturePath, SliceSyntax Slice)> AllSlices(
            string module,
            ImmutableArray<string> parentPath,
            IEnumerable<FeatureSyntax> features)
        {
            foreach (var feature in features)
            {
                var path = parentPath.Add(feature.Name);
                foreach (var slice in feature.Slices)
                {
                    yield return (module, path, slice);
                }

                foreach (var nested in AllSlices(module, path, feature.Features))
                {
                    yield return nested;
                }
            }
        }

        sealed record BoundEvent(
            EventSyntax Syntax,
            SemanticEventContract Contract,
            Dictionary<string, SemanticProperty> Properties);

        sealed record BoundReadModel(
            ReadModelSyntax Syntax,
            SemanticReadModel Model,
            Dictionary<string, SemanticProperty> Properties);
    }
}
