// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    // Deliberately does not create an ExecutableSemanticModel or run the v1-v34 validators/serializer on a v35 graph.
    internal CompilationResult<PossibleEventsBinding> BindForPossibleEvents(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        var detector = new PossibleEventsDetector();
        detector.VisitApplication(syntax);
        if (!detector.Found) return BindLegacyPossibleEvents(applicationName, syntax, documents);

        var admission = CommandProductionAdmission(syntax, out _);
        if (admission.Count > 0) return CompilationResult<PossibleEventsBinding>.Failed(admission);
        var expansion = SpecificationExamples.Expand(syntax);
        if (expansion.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) return CompilationResult<PossibleEventsBinding>.Failed(expansion.Diagnostics);
        var context = new BindingContext(applicationName, expansion.Application, documents, PossibleEventSteps(expansion), expansion, _utcAssessmentYear ?? DateTimeOffset.UtcNow.Year) { AllowPossibleEvents = true };
        try
        {
            var application = context.BindApplication();
            var requirements = context.ImplementationRequirements;
            var descriptors = SemanticTypedContextCatalog.Create(application, requirements, strict: !context.HasErrors);
            var versions = SelectBoundVersions(application, SemanticVersion.V1, context.UsesStoredEventNames) ?? (LanguageVersion.V1, SemanticVersion.V1);
            return new(context.HasErrors ? null : new(application, versions.Language, versions.Semantic), context.Diagnostics)
            {
                ImplementationRequirements = requirements,
                TypedContextDescriptors = descriptors
            };
        }
        catch (InvalidSemanticContract exception)
        {
            return CompilationResult<PossibleEventsBinding>.Failed(context.Diagnostics.Append(Diagnostic.Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, syntax.Location))) with
            {
                ImplementationRequirements = context.ImplementationRequirements
            };
        }
    }

    static Dictionary<SyntaxNode, EffectiveSpecificationStep> PossibleEventSteps(EffectiveSpecificationApplication expansion)
    {
        var steps = new Dictionary<SyntaxNode, EffectiveSpecificationStep>(ReferenceEqualityComparer.Instance);
        foreach (var step in expansion.Specifications.SelectMany(specification => specification.Steps)) steps.TryAdd(step.Effective, step);
        return steps;
    }

    static IEnumerable<SemanticSlice> PossibleEventSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(PossibleEventSlices));

    CompilationResult<PossibleEventsBinding> BindLegacyPossibleEvents(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        var legacy = Bind(applicationName, syntax, documents);
        return new(legacy.Value is { } compilation ? new(compilation.Model.Application, compilation.Model.LanguageVersion, compilation.Model.SemanticVersion) : null, legacy.Diagnostics)
        {
            ImplementationRequirements = legacy.ImplementationRequirements,
            TypedContextDescriptors = legacy.TypedContextDescriptors
        };
    }

    sealed class PossibleEventsDetector : ScreenplaySyntaxWalker
    {
        internal bool Found { get; private set; }
        public override void VisitPossibleEvents(PossibleEventsSyntax syntax) => Found = true;
    }
}
