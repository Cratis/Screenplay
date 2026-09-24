// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds compatible source syntax to ESM v1 and reports every non-bound construct explicitly.
/// </summary>
public sealed partial class SemanticModelBinder : ISemanticModelBinder
{
    /// <inheritdoc/>
    public CompilationResult<SemanticCompilation> Bind(
        string applicationName,
        ApplicationSyntax syntax,
        SemanticDocumentSet documents)
    {
        var context = new BindingContext(applicationName, syntax, documents);
        try
        {
            var application = context.BindApplication();
            if (context.HasErrors)
            {
                return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics);
            }

            var version = context.UsesV2;
            var model = ExecutableSemanticModel.Create(
                version ? LanguageVersion.V2 : LanguageVersion.V1,
                version ? SemanticVersion.V2 : SemanticVersion.V1,
                application);
            var sourceMap = SemanticSourceMap.Create(context.SourceMapEntries, documents.Documents);
            var compilation = SemanticCompilation.Create(model, documents, sourceMap);
            return new(compilation, context.Diagnostics);
        }
        catch (InvalidSemanticContract exception)
        {
            context.Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, syntax.Location);
            return CompilationResult<SemanticCompilation>.Failed(context.Diagnostics);
        }
    }

    private sealed partial class BindingContext(string applicationName, ApplicationSyntax syntax, SemanticDocumentSet documents)
    {
        readonly List<Diagnostic> _diagnostics = [];
        readonly List<SemanticSourceMapEntry> _sourceMapEntries = [];
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _concepts = new(StringComparer.Ordinal);
        readonly Dictionary<EventSyntax, BoundEvent> _eventDeclarations = [];
        readonly Dictionary<string, BoundEvent> _events = new(StringComparer.Ordinal);
        readonly Dictionary<QuerySyntax, SemanticKeyedQuery> _queryDeclarations = [];
        readonly Dictionary<string, SemanticKeyedQuery> _queries = new(StringComparer.Ordinal);
        readonly Dictionary<ReadModelSyntax, BoundReadModel> _readModelDeclarations = [];
        readonly Dictionary<string, BoundReadModel> _readModels = new(StringComparer.Ordinal);
        readonly Dictionary<string, (SemanticAddress Address, SemanticId Id)> _types = new(StringComparer.Ordinal);
        readonly ApplicationIdentity _applicationIdentity = documents.IdentityCatalog.Application;

        internal IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        internal bool HasErrors => _diagnostics.Exists(_ => _.Severity == DiagnosticSeverity.Error);

        internal bool UsesV2 { get; set; }

        internal ImmutableArray<SemanticSourceMapEntry> SourceMapEntries => [.. _sourceMapEntries];

        internal SemanticApplication BindApplication()
        {
            ReportTopLevelDispositions();
            var applicationAddress = SemanticAddress.ForApplication(_applicationIdentity);
            var applicationId = Resolve(applicationAddress, syntax.Location);

            RegisterTypeDeclarations();
            RegisterEventDeclarations();
            RegisterReadModelDeclarations();
            RegisterQueryDeclarations();
            var concepts = syntax.Concepts.Select(BindConcept).ToImmutableArray();
            var types = (syntax.Types ?? []).Select(BindType).ToImmutableArray();
            var modules = syntax.Modules.Select(BindModule).ToImmutableArray();
            return new(
                applicationId,
                applicationName,
                concepts,
                types,
                UsesV2 ? [.. modules.Select(PromoteV2Destinations)] : modules);
        }

        internal void Error(string code, string message, SourceLocation location) =>
            _diagnostics.Add(Diagnostic.Error(code, message, location));

        static SemanticModule PromoteV2Destinations(SemanticModule module) =>
            module with { Features = [.. module.Features.Select(PromoteV2Destinations)] };

        static SemanticFeature PromoteV2Destinations(SemanticFeature feature) => feature with
        {
            Features = [.. feature.Features.Select(PromoteV2Destinations)],
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(command =>
                {
                    var source = command.Produces.Select(produced => produced.Destination)
                        .OfType<SemanticResolvedExpression>().FirstOrDefault();
                    if (command.Destination is not null || source is null) return command;
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

            foreach (var policy in syntax.Policies)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' is deferred until portable policy semantics are admitted.", policy.Location);
            }

            foreach (var persona in syntax.Personas ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Persona '{persona.Name}' is deferred from ESM v1.", persona.Location);
            }

            if (syntax.Authentication is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Authentication providers are realization metadata and are not part of ESM v1 behavior.", syntax.Authentication.Location);
            }

            foreach (var seed in syntax.Seeds ?? [])
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, "Event seeding is operational metadata and is not part of ESM v1 behavior.", seed.Location);
            }

            foreach (var trigger in syntax.Triggers ?? [])
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Trigger '{trigger.Name}' is deferred until portable occurrence semantics are admitted.", trigger.Location);
            }

            foreach (var profile in syntax.UiProfiles ?? [])
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"UI profile '{profile.Name}' is explicitly deferred from the backend ESM v1 profile.", profile.Location);
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
                foreach (var @event in slice.Events)
                {
                    var bound = BindEvent(sliceAddress, @event);
                    _eventDeclarations.Add(@event, bound);
                    if (!_events.TryAdd(@event.Name, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Event reference '{@event.Name}' is ambiguous across slices in the current ESM v1 binder.", @event.Location);
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

                    _queryDeclarations.Add(query, bound);
                    if (!_queries.TryAdd(query.Name, bound))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Query reference '{query.Name}' is ambiguous across slices in the current ESM v1 binder.", query.Location);
                    }
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
