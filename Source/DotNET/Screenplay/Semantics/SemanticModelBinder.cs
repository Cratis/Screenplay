// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Extensions;
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
        var resolution = ExtensionContractResolver.Current.Resolve(syntax, documents.Documents.Select(document => document.DisplayPath));
        var effective = resolution.Apply(syntax);
        var result = BindCore(applicationName, effective, documents);
        var extensionDiagnostics = resolution.Diagnostics.AddRange(new ExtensionUseSites(resolution).Check(syntax)).AddRange(resolution.CheckSettings(syntax));
        var diagnostics = DiagnosticSubjects.Attach(syntax, [.. extensionDiagnostics, .. result.Diagnostics]);
        var partial = extensionDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ? null : result.SpecificationRefusedValue;
        if (partial is null)
        {
            return result with { Diagnostics = diagnostics, SpecificationRefusedValue = null };
        }

        partial = AttachRefusalDiagnostics(partial, result.Diagnostics, diagnostics.Skip(extensionDiagnostics.Length));
        return result with
        {
            Diagnostics = diagnostics,
            SpecificationRefusedValue = partial,
            SpecificationRefusedTypedContextDescriptors = DescriptorsFor(partial, SemanticTypedContextCatalog.Create(partial.Model.Application, result.ImplementationRequirements, true))
        };
    }

    /// <summary>
    /// Stamps typed descriptors with the revision of their validated compilation.
    /// </summary>
    static ImmutableArray<SemanticTypedContextDescriptor> DescriptorsFor(SemanticCompilation compilation, ImmutableArray<SemanticTypedContextDescriptor> descriptors) =>
        [.. descriptors.Select(value => value with { ModelRevision = compilation.Model.Revision })];

    static SemanticCompilation AttachRefusalDiagnostics(SemanticCompilation compilation, IEnumerable<Diagnostic> originals, IEnumerable<Diagnostic> attached)
    {
        Dictionary<Diagnostic, Diagnostic> diagnostics = new(ReferenceEqualityComparer.Instance);
        foreach (var (original, diagnostic) in originals.Zip(attached)) diagnostics[original] = diagnostic;
        return compilation.WithRefusedSpecifications([.. compilation.RefusedSpecifications.Select(refusal => refusal with { Diagnostics = [.. refusal.Diagnostics.Select(diagnostic => diagnostics[diagnostic])] })]);
    }

    // Every refusal of a model that no executable model admits, in report order: command productions, agent exposure, read-model
    // member types, identity details, then public-event boundaries. Any diagnostic collected here, of any severity, refuses the model.
    static List<Diagnostic> CollectAdmissionDiagnostics(ApplicationSyntax syntax)
    {
        var admission = CommandProductionAdmission(syntax, out var usesPublicEvents);
        var memberTypes = ParserContext.ForDiagnostics();
        var agents = ParserContext.ForDiagnostics();
        AgentsValidator.Validate(syntax, agents, deferImportedReferences: false);
        foreach (var diagnostic in agents.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) memberTypes.Add(diagnostic);
        ReadModelMemberTypeValidator.Validate(syntax, memberTypes, rejectUnknownTypes: true);
        admission.AddRange(memberTypes.Diagnostics);
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

        return admission;
    }

    static CompilationResult<SemanticCompilation>? RefuseUnsupportedSyntax(ApplicationSyntax syntax)
    {
        if (syntax.ReactionProductionMode is { } mode && mode != ReactionProductionMode.Atomic)

        {
            return CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(DiagnosticCodes.InvalidReactionBatchesDeclaration, "Declare 'reaction batches atomic' once at the application root.", syntax.Location)]);
        }

        var numeric = SourceNumericModes.Errors(syntax);
        if (numeric.Count > 0)

        {
            return CompilationResult<SemanticCompilation>.Failed(numeric);
        }

        if (syntax.SourceOptions.NumericMode == NumericMode.Exact)

        {
            return CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(
                DiagnosticCodes.UnsupportedSemanticSyntax,
                "Exact numeric source mode is not admitted by any supported executable model (ESM) version yet (#285); this source/syntax implementation does not execute exact documents.",
                syntax.Location,
                DiagnosticArguments.ForIssue("Cratis/Screenplay#285"))]);
        }

        var admission = CollectAdmissionDiagnostics(syntax);
        return admission.Count > 0 ? CompilationResult<SemanticCompilation>.Failed(admission) : null;
    }

    static CompilationResult<SemanticCompilation> CompileBoundApplication(BindingContext context, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        var application = context.BindApplication();
        application = application with { Agents = context.BindAgents(application) };
        var failureDescriptors = SemanticTypedContextCatalog.Create(application, context.ImplementationRequirements, !context.HasErrors);
        if (context.HasStructuralErrors)

        {
            return FailedBinding(context, failureDescriptors);
        }

        var prepared = ApplyRequiredVersions(context, application, failureDescriptors);
        var (languageVersion, semanticVersion, failures) = ValidateAtRequiredVersion(prepared.Application, prepared.Language, prepared.Semantic, context.UtcAssessmentYear);
        if (!failures.IsEmpty)

        {
            ReportValidationFailures(context, failures);
            return FailedBinding(context, prepared.Descriptors);
        }

        var model = ExecutableSemanticModel.CreateValidated(languageVersion, semanticVersion, prepared.Application);
        var compilation = AssembleCompilation(context, syntax, documents, model);
        return context.CompleteCompilation(compilation, prepared.Descriptors, failureDescriptors);
    }

    static void ReportValidationFailures(BindingContext context, ImmutableArray<SemanticModelValidator.SemanticValidationFailure> failures)
    {
        foreach (var failure in failures)

        {
            context.Error(DiagnosticCodes.InvalidSemanticBinding, failure.Message, context.FailureLocation(failure));
        }
    }

    static CompilationResult<SemanticCompilation> FailedBinding(BindingContext context, ImmutableArray<SemanticTypedContextDescriptor> descriptors) =>
        CompilationResult<SemanticCompilation>.Failed(context.Diagnostics) with
        {
            ImplementationRequirements = context.ImplementationRequirements,
            TypedContextDescriptors = descriptors
        };

    static SemanticCompilation AssembleCompilation(BindingContext context, ApplicationSyntax syntax, SemanticDocumentSet documents, ExecutableSemanticModel model)
    {
        var compiledDocuments = context.UsesV4
            ? BootstrapEventRevisions(documents, model.Application, context.RefusedSpecifications)
            : documents;
        var sourceMap = SemanticSourceMap.Create(context.SourceMapEntries, compiledDocuments.Documents);
        return SemanticCompilation.Create(model, compiledDocuments, sourceMap, context.RefusedSpecifications)
            .WithSpecificationOrigins(context.SpecificationOrigins)
            .WithEnvironment(EnvironmentRequirements.For(syntax))
            .WithPassiveReadModels(context.PassiveReadModels)
            .WithSinks(context.Sinks, context.ReadModelSinks, context.GraphSinks)
            .WithSystemConfigurations(context.SystemConfigurations, context.Secrets);
    }

    CompilationResult<SemanticCompilation> BindCore(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        if (RefuseUnsupportedSyntax(syntax) is { } refusal)

        {
            return refusal;
        }

        var expansion = SpecificationExamples.Expand(syntax);
        if (expansion.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))

        {
            return CompilationResult<SemanticCompilation>.Failed(expansion.Diagnostics);
        }

        return BindExpandedApplication(applicationName, expansion, documents);
    }

    CompilationResult<SemanticCompilation> BindExpandedApplication(string applicationName, EffectiveSpecificationApplication expansion, SemanticDocumentSet documents)
    {
        var syntax = expansion.Application;
        var steps = new Dictionary<SyntaxNode, EffectiveSpecificationStep>(ReferenceEqualityComparer.Instance);
        foreach (var step in expansion.Specifications.SelectMany(specification => specification.Steps))

        {
            steps.TryAdd(step.Effective, step);
        }

        var context = new BindingContext(applicationName, syntax, documents, steps, expansion, _utcAssessmentYear ?? DateTimeOffset.UtcNow.Year);
        try

        {
            return CompileBoundApplication(context, syntax, documents);
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
        readonly Dictionary<SpecificationSyntax, (SemanticId Id, EffectiveSpecification? Origin)> _specificationBindings = new(ReferenceEqualityComparer.Instance);
        readonly Dictionary<SemanticId, ImmutableArray<SemanticId>> _specificationSources = [];
        readonly Dictionary<SemanticId, (EffectiveSpecification? Origin, ImmutableArray<int> Diagnostics)> _refusedSpecifications = [];
        readonly HashSet<int> _specificationErrorIndexes = [];
        readonly List<SemanticSourceMapEntry> _sourceMapEntries = [];
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _concepts = new(StringComparer.Ordinal);
        readonly Dictionary<EventSyntax, BoundEvent> _eventDeclarations = [];
        readonly Dictionary<string, BoundEvent> _events = new(StringComparer.Ordinal);
        readonly Dictionary<QuerySyntax, SemanticKeyedQuery> _queryDeclarations = [];
        readonly Dictionary<ReadModelSyntax, BoundReadModel> _readModelDeclarations = [];
        readonly HashSet<SemanticId> _passiveReadModels = [];
        readonly Dictionary<SemanticId, string> _readModelSinks = [];
        readonly Dictionary<string, string> _graphSinks = new(StringComparer.Ordinal);
        readonly Dictionary<string, BoundReadModel> _readModels = new(StringComparer.Ordinal);
        readonly Dictionary<string, string[]> _readModelPaths = new(StringComparer.Ordinal);
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _types = new(StringComparer.Ordinal);
        readonly ApplicationIdentity _applicationIdentity = documents.IdentityCatalog.Application;

        internal IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        internal ImmutableDictionary<SemanticId, EffectiveSpecification> SpecificationOrigins => _specificationOrigins.Where(pair => !_refusedSpecifications.ContainsKey(pair.Key)).ToImmutableDictionary();

        internal ImmutableArray<RefusedSpecification> RefusedSpecifications =>
            [.. _refusedSpecifications.Select(pair => new RefusedSpecification(pair.Key, [.. pair.Value.Diagnostics.Select(index => _diagnostics[index])], pair.Value.Origin)
            {
                DeclarationIds = _specificationSources.GetValueOrDefault(pair.Key, [])
            })];

        internal ImmutableHashSet<SemanticId> PassiveReadModels => [.. _passiveReadModels];

        internal ImmutableArray<SemanticSink> Sinks => [.. syntax.Sinks.Select(sink => new SemanticSink(sink.Name, sink.Stores == SinkKind.Graphs ? SemanticSinkKind.Graphs : SemanticSinkKind.ReadModels))];

        internal ImmutableArray<SemanticSecretDeclaration> Secrets => [.. syntax.Secrets.Select(secret => new SemanticSecretDeclaration(secret.Name, secret.Description))];

        internal ImmutableArray<SemanticSystemConfiguration> SystemConfigurations => [.. syntax.Systems
            .Where(system => system.Extension is not null || system.Settings.Any() || system.Activation is not null)
            .Select(system => new SemanticSystemConfiguration(
                system.Name,
                system.Extension?.Reference,
                system.Extension?.Version,
                [.. system.Settings.Select(setting => new SemanticSystemSetting(setting.Name, setting.Secret, setting.Value))],
                system.Activation is { } activation ? new SemanticSystemActivation(activation.Always, activation.IdleAmount, activation.IdleUnit) : null))];

        internal ImmutableDictionary<SemanticId, string> ReadModelSinks => _readModelSinks.ToImmutableDictionary();

        internal ImmutableDictionary<string, string> GraphSinks => _graphSinks.ToImmutableDictionary(StringComparer.Ordinal);

        internal int UtcAssessmentYear => utcAssessmentYear;

        internal bool HasErrors => _diagnostics.Exists(_ => _.Severity == DiagnosticSeverity.Error);

        internal bool HasStructuralErrors => _diagnostics.Where((diagnostic, index) => diagnostic.Severity == DiagnosticSeverity.Error && !_specificationErrorIndexes.Contains(index)).Any();

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

        internal ImmutableArray<SemanticSourceMapEntry> SourceMapEntries
        {
            get
            {
                var refusedSources = _refusedSpecifications.Keys.SelectMany(id => _specificationSources.GetValueOrDefault(id, [])).ToHashSet();
                return [.. _sourceMapEntries.Where(entry => !refusedSources.Contains(entry.SemanticId))];
            }
        }

        internal static SemanticApplication PromoteV2Destinations(SemanticApplication application) =>
            application with { Modules = [.. application.Modules.Select(PromoteV2Destinations)] };

        // A clock model states its effective zone; an undeclared one is non-configurable UTC (decision 0106), so canonical bytes round-trip.
        internal static SemanticApplication WithEffectiveTimeZone(SemanticApplication application) =>
            application.TimeZone is null && SemanticClockUsage.Uses(application) ? application with { TimeZone = new("UTC", false) } : application;

        internal SemanticApplication BindApplication()
        {
            ReportAuthoringDispositions();
            ReportRealizationDispositions();
            ReportDeferredUiDispositions();
            ReportDeferredIntegrationDispositions();
            ReportPurposeDispositions();
            var applicationAddress = SemanticAddress.ForApplication(_applicationIdentity);
            var applicationId = Resolve(applicationAddress, syntax.Location);

            ValidateComplianceAdmission();
            ValidateClockOperandAdmission();
            var timeZone = BindApplicationTimeZone();
            BindApplicationContext();

            var triggers = RegisterDeclarations();
            return FinalizeApplication(BindApplicationBody(applicationId, timeZone, triggers));
        }

        internal void Error(string code, string message, SourceLocation location, DiagnosticArguments? arguments = null)
        {
            var caseValue = expansion.Specifications
                .Select(specification => specification.Case)
                .OfType<SpecificationCaseSyntax>()
                .SelectMany(@case => @case.Values.Select(value => (@case.Name, Value: value)))
                .FirstOrDefault(value => value.Value.Source.Location.Line > 1 && value.Value.Source.Location == location);
            if (caseValue.Value is { } parameter)

            {
                message = CaseDiagnosticMessage(caseValue.Name, parameter.Property, message);
                arguments = CaseDiagnosticArguments(caseValue.Name, parameter.Property, code, arguments);
            }

            _diagnostics.Add(Diagnostic.Error(code, message, location, arguments ?? DiagnosticArguments.Empty));
        }

        static string CaseDiagnosticMessage(string caseName, string parameter, string message) =>
            message.StartsWith("Case '", StringComparison.Ordinal) ? message : $"Case '{caseName}' parameter '{parameter}': {message}";

        static DiagnosticArguments? CaseDiagnosticArguments(string caseName, string parameter, string code, DiagnosticArguments? arguments) =>
            string.Equals(code, DiagnosticCodes.MissingSpecificationProperty, StringComparison.Ordinal) ||
            string.Equals(code, DiagnosticCodes.InvalidSpecificationCaseValue, StringComparison.Ordinal)
                ? (arguments ?? DiagnosticArguments.Empty).With("case", DiagnosticArgument.Text(caseName)).With("parameter", DiagnosticArgument.Text(parameter))
                : arguments;

        static SemanticModule PromoteV2Destinations(SemanticModule module) =>
            module with { Features = [.. module.Features.Select(PromoteV2Destinations)] };

        static SemanticFeature PromoteV2Destinations(SemanticFeature feature) => feature with
        {
            Features = [.. feature.Features.Select(PromoteV2Destinations)],
            Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(PromoteV2Destination)] })]
        };

        static SemanticCommand PromoteV2Destination(SemanticCommand command)
        {
            var source = command.Produces.Where(produced => CanPromoteDestination(command, produced))
                .Select(produced => produced.Destination).OfType<SemanticResolvedExpression>().FirstOrDefault();
            if (command.Destination is not null || source is null || MustKeepGeneratedDestination(command))

            {
                return command;
            }

            var property = command.Properties.Single(value => value.Id == source.Target);
            return command with { Destination = new(property.Type, source) };
        }

        static bool CanPromoteDestination(SemanticCommand command, SemanticProducedEvent produced) =>
            produced.Route is null || (produced.Destination is SemanticResolvedExpression expression &&
                command.Properties.Any(property => property.Id == expression.Target && property.IsIdentifier));

        static bool MustKeepGeneratedDestination(SemanticCommand command) =>
            command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null);

        ImmutableArray<SemanticApplicationTrigger> RegisterDeclarations()
        {
            RegisterTypeDeclarations();
            var triggers = RegisterTriggerDeclarations();
            RegisterEventDeclarations();
            RegisterReadModelDeclarations();
            RegisterQueryDeclarations();
            return triggers;
        }

        SemanticApplication BindApplicationBody(SemanticId applicationId, SemanticTimeZone? timeZone, ImmutableArray<SemanticApplicationTrigger> triggers)
        {
            var concepts = syntax.Concepts.Select(BindConcept).ToImmutableArray();
            _boundConcepts = concepts.GroupBy(concept => concept.Id).ToDictionary(group => group.Key, group => group.First());
            var types = (syntax.Types ?? []).Select(BindType).ToImmutableArray();
            var eventSources = BindEventSources(concepts);
            if (syntax.CommandReadMode is not null)

            {
                RegisterDecisionReadProjections(concepts);
            }

            var modules = AttachAutomation([.. syntax.Modules.Select(BindModule)]);
            var (stateMachines, workflows) = BindLifecycles(modules);
            var (graphs, graphProjections, graphSpecifications, graphQueries) = BindGraphs(modules);
            var systems = BindSystems();
            var policies = BindPolicies();
            VerifyPersonaCallers(policies);
            ValidateReactionIdentities(modules, policies);
            return new SemanticApplication(
                applicationId,
                applicationName,
                concepts,
                types,
                UseVersionedCommandDestinations() || !eventSources.IsEmpty ? [.. modules.Select(PromoteV2Destinations)] : modules)
            {
                Policies = policies,
                Audiences = SemanticAvailabilityBinder.Audiences(syntax.Audiences),
                Availabilities = SemanticAvailabilityBinder.Availabilities(syntax.Availabilities),
                Provides = BindProvides(SemanticAddress.ForApplication(_applicationIdentity), syntax.Provides),
                Triggers = triggers,
                Systems = systems,
                StateMachines = stateMachines,
                Workflows = workflows,
                Graphs = graphs,
                GraphProjections = graphProjections,
                GraphSpecifications = graphSpecifications,
                GraphQueries = graphQueries,
                EventSources = eventSources,
                ReactionProductionMode = syntax.ReactionProductionMode is null ? null : SemanticReactionProductionMode.Atomic,
                TimeZone = timeZone,
                ContextMembers = [.. _contextMembers],
                CommandReadMode = syntax.CommandReadMode is null ? null : SemanticCommandReadMode.Protected
            };
        }

        bool UseVersionedCommandDestinations() =>
            this is { UsesV2: true } or { UsesV3: true } or { UsesV4: true } or { UsesV5: true }
                or { UsesV6: true } or { UsesV7: true } or { UsesPublicEvents: true } or { UsesV10: true };

        SemanticApplication FinalizeApplication(SemanticApplication application)
        {
            application = WithoutRefusedSpecifications(application);
            ValidateExampleAdmission(application);
            application = WithEffectiveTimeZone(BindRoutedFixtureSources(WithoutRefusedSpecifications(application)));
            application = WithRetainedSpecificationVersions(application);
            UsesEventRoutes = SemanticEventRouting.Uses(application);
            ReportLegacyDecisionReads();

            return application;
        }

        void RecordSpecificationErrors(SemanticId id, EffectiveSpecification? origin, int start)
        {
            var errors = Enumerable.Range(start, _diagnostics.Count - start).Where(index => _diagnostics[index].Severity == DiagnosticSeverity.Error).ToImmutableArray();
            if (errors.IsEmpty) return;
            _specificationErrorIndexes.UnionWith(errors);
            var previous = _refusedSpecifications.TryGetValue(id, out var refusal) ? refusal.Diagnostics : [];
            _refusedSpecifications[id] = (origin, previous.AddRange(errors));
        }

        SemanticApplication WithoutRefusedSpecifications(SemanticApplication application) =>
            _refusedSpecifications.Count == 0 ? application : application with
            {
                Modules = [.. application.Modules.Select(module => module with
                {
                    Features = [.. module.Features.Select(WithoutRefusedSpecifications)]
                })]
            };

        SemanticFeature WithoutRefusedSpecifications(SemanticFeature feature) => feature with
        {
            Features = [.. feature.Features.Select(WithoutRefusedSpecifications)],
            Slices = [.. feature.Slices.Select(slice => slice with { Specifications = [.. slice.Specifications.Where(specification => !_refusedSpecifications.ContainsKey(specification.Id))] })]
        };

        void Information(string code, string message, SourceLocation location) =>
            _diagnostics.Add(new(DiagnosticSeverity.Information, code, message, location));

        void ReportAuthoringDispositions()
        {
            if (syntax.Domain is { } domain)

            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "A domain declaration is authoring metadata and is not part of ESM v1 behavior.", domain.Location);
            }

            foreach (var import in syntax.Imports)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Import '{import.QualifiedName}' is not supported by ESM v1 binding.", import.Location);
            }

            var knownPolicies = syntax.Policies.Select(policy => policy.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var persona in syntax.Personas ?? [])

            {
                ReportPersonaDisposition(persona, knownPolicies);
            }
        }

        void ReportPersonaDisposition(PersonaSyntax persona, HashSet<string> knownPolicies)
        {
            foreach (var policy in persona.Policies.Where(policy => !knownPolicies.Contains(policy)))

            {
                Error(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
            }

            Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Persona '{persona.Name}' is authoring metadata and is not part of ESM v1 behavior.", persona.Location);
        }

        void ReportRealizationDispositions()
        {
            const string Code = DiagnosticCodes.ReportOnlySemanticSyntax;
            ReportInformation(syntax.Sinks, Code, sink => $"Sink '{sink.Name}' is realization metadata and is not part of ESM behavior.");

            if (syntax.Authentication is { } authentication)

            {
                Information(Code, "Authentication providers are realization metadata and are not part of ESM v1 behavior.", authentication.Location);
            }

            if (syntax.Onboarding is { } onboarding)

            {
                Information(Code, "Onboarding is realization metadata and is not part of ESM v1 behavior.", onboarding.Location);
            }

            ReportInformation(syntax.Seeds ?? [], Code, _ => "Event seeding is operational metadata and is not part of ESM v1 behavior.");
        }

        void ReportDeferredUiDispositions()
        {
            const string Code = DiagnosticCodes.DeferredSemanticSyntax;
            ReportInformation(syntax.UiProfiles ?? [], Code, profile => $"UI profile '{profile.Name}' is explicitly deferred from the backend ESM v1 profile.");
            ReportInformation(syntax.SampleDataPacks, Code, pack => $"Sample data pack '{pack.Name}' is design and preview time only and is explicitly deferred from the backend ESM v1 profile.");
            ReportInformation(syntax.DesignSystems, Code, designSystem => $"Design system '{designSystem.Name}' is UI configuration and is explicitly deferred from the backend ESM v1 profile.");
            ReportInformation(syntax.Themes ?? [], Code, theme => $"Theme '{theme.Name}' is explicitly deferred from the backend ESM v1 profile.");
            ReportInformation(syntax.Layouts ?? [], Code, layout => $"Layout '{layout.Name}' is explicitly deferred from the backend ESM v1 profile.");

            // A behavior is UI behavior: the backend profile does not run it, the way it does not run a screen.
            // Deferred is not dropped - the syntax tree, the printer and the UI consumers carry it in full.
            ReportInformation(syntax.Behaviors, Code, behavior => $"Behavior '{behavior.Name}' is explicitly deferred from the backend ESM v1 profile.");
        }

        void ReportDeferredIntegrationDispositions()
        {
            const string Code = DiagnosticCodes.DeferredSemanticSyntax;
            ReportInformation(syntax.Exposures, Code, exposure => $"Exposure for '{exposure.Owner}' is explicitly deferred from the backend ESM v1 profile.");
            ReportInformation(syntax.InstanceContributions, Code, instance => $"Instance contributions for '{instance.Instance}' are explicitly deferred from the backend ESM v1 profile.");
        }

        void ReportPurposeDispositions()
        {
            const string Code = DiagnosticCodes.ReportOnlySemanticSyntax;
            ReportInformation(syntax.Purposes, Code, purpose => $"Purpose '{purpose.Name}' is report-only processing metadata and adds no executable behavior.");
            ReportInformation(PurposeReferences(syntax), Code, reference => $"Purpose reference '{reference.Name}' is report-only processing metadata.");
        }

        void ReportInformation<TSyntax>(IEnumerable<TSyntax> declarations, string code, Func<TSyntax, string> message)
            where TSyntax : SyntaxNode
        {
            foreach (var declaration in declarations)

            {
                Information(code, message(declaration), declaration.Location);
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
                    if (readModel.IsPassive) _passiveReadModels.Add(bound.Model.Id);
                    if (readModel.Sink is { } sink) _readModelSinks.TryAdd(bound.Model.Id, sink);
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
                foreach (var query in slice.Queries.Where(CanBindPerformedQueryArguments))
                {
                    RegisterQueryDeclaration(sliceAddress, query, module, featurePath);
                }
            }
        }

        void RegisterQueryDeclaration(SemanticAddress sliceAddress, QuerySyntax query, string module, ImmutableArray<string> featurePath)
        {
            if (BindQuery(sliceAddress, query) is { } bound)

            {
                _queryDeclarations.Add(query, bound with
                {
                    Authorization = EffectiveAuthorization(
                        query.Authorize,
                        SemanticQueryArguments.KeyProperties(bound),
                        module,
                        featurePath,
                        queryFilterNames: query.Filters.Select(filter => filter.Name)),
                    Available = SemanticAvailabilityBinder.Gates(query.Available)
                });
            }
            else if (query.Performer is not null)

            {
                AuthorizePerformedQuery(query, module, featurePath);
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
            Dictionary<string, SemanticProperty> Properties)
        {
            /// <summary>Gets the declared read model's address; null when the bound shape is a public event.</summary>
            public SemanticAddress? Address { get; init; }
        }
    }
}
