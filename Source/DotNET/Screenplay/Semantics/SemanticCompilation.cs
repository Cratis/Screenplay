// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a validated executable semantic model and its source integrity metadata.
/// </summary>
public sealed class SemanticCompilation
{
    SemanticCompilation(ExecutableSemanticModel model, SemanticDocumentSet documents, SemanticSourceMap sourceMap)
    {
        Model = model;
        Documents = documents;
        SourceMap = sourceMap;
    }

    /// <summary>
    /// Gets the executable semantic model.
    /// </summary>
    public ExecutableSemanticModel Model { get; }

    /// <summary>
    /// Gets the logical source document set and authoritative identity catalog.
    /// </summary>
    public SemanticDocumentSet Documents { get; }

    /// <summary>
    /// Gets the source-to-semantic map.
    /// </summary>
    public SemanticSourceMap SourceMap { get; }

    /// <summary>
    /// Gets the source-bound effective specifications and value origins. This sidecar is not part of ESM bytes.
    /// </summary>
    public ImmutableDictionary<SemanticId, EffectiveSpecification> SpecificationOrigins { get; private init; } = ImmutableDictionary.Create<SemanticId, EffectiveSpecification>();

    /// <summary>
    /// Gets the environment values the model needs to run - one per provider setting and per onboarding mail setting.
    /// This sidecar is not part of ESM bytes: providers and onboarding are realization metadata, and this is how a
    /// runtime reads what they need from a compilation without parsing the source again.
    /// </summary>
    public EnvironmentRequirements Environment { get; private init; } = EnvironmentRequirements.None;

    /// <summary>
    /// Gets the explicitly passive read-model identities, or an empty set when none were authored.
    /// This realization sidecar is not part of ESM bytes or its revision. Renderers must include it
    /// in hashed render input, and runtimes must carry it through application realization.
    /// </summary>
    public ImmutableHashSet<SemanticId> PassiveReadModels { get; private init; } = [];

    /// <summary>
    /// Gets the named sinks the application declares, in declaration order, or an empty array when it declares none.
    /// Every application also has an implicit default sink per kind, which is never listed. This realization sidecar is
    /// not part of ESM bytes or its revision; renderers must include it in hashed render input.
    /// </summary>
    public ImmutableArray<SemanticSink> Sinks { get; private init; } = [];

    /// <summary>
    /// Gets the sink name of every read model that names one, keyed by the read model's canonical identity. A read model
    /// that is absent uses the default read-model sink; its projection or reducer writes there and its queries read
    /// from there. This realization sidecar is not part of ESM bytes or its revision.
    /// </summary>
    public ImmutableDictionary<SemanticId, string> ReadModelSinks { get; private init; } = ImmutableDictionary.Create<SemanticId, string>();

    /// <summary>
    /// Gets the sink name of every graph that names one, keyed by the graph's canonical identity, its name. A graph that
    /// is absent uses the default graph sink. This realization sidecar is not part of ESM bytes or its revision.
    /// </summary>
    public ImmutableDictionary<string, string> GraphSinks { get; private init; } = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal);

    /// <summary>
    /// Creates a compilation after cross-checking the model, identity catalog, documents, and source map.
    /// </summary>
    /// <param name="model">The validated executable semantic model.</param>
    /// <param name="documents">The validated logical source document set.</param>
    /// <param name="sourceMap">The source-to-semantic map.</param>
    /// <returns>The validated semantic compilation.</returns>
    /// <exception cref="InvalidSemanticContract">The model, catalog, documents, or source map are inconsistent.</exception>
    public static SemanticCompilation Create(
        ExecutableSemanticModel model,
        SemanticDocumentSet documents,
        SemanticSourceMap sourceMap)
    {
        if (model is null || documents is null || sourceMap is null)
        {
            throw new InvalidSemanticContract("A semantic compilation requires a model, document set, and source map.");
        }

        var index = SemanticCompilationIndex.Create(model.Application, documents.IdentityCatalog.Application);
        var documentKeys = documents.Documents.Select(_ => _.StableKey).ToImmutableArray();
        var addresses = index.Declarations.Keys.ToImmutableArray();
        var eventAddresses = index.Events.Keys.ToImmutableArray();
        documents.IdentityCatalog.VerifyAgainst(documentKeys, addresses, eventAddresses);

        foreach (var declaration in index.Declarations)
        {
            if (documents.IdentityCatalog.ResolveSemantic(declaration.Key) != declaration.Value)
            {
                throw new InvalidSemanticContract($"Semantic declaration '{declaration.Value}' disagrees with its identity catalog assignment.");
            }
        }

        foreach (var eventContract in index.Events)
        {
            var assignment = documents.IdentityCatalog.ResolveEventContract(eventContract.Key);
            if (assignment.Id != eventContract.Value.ContractId || assignment.Revision != eventContract.Value.Revision)
            {
                throw new InvalidSemanticContract($"Event contract '{eventContract.Value.Id}' disagrees with its identity catalog assignment.");
            }
        }

        var validatedSourceMap = SemanticSourceMap.Create(sourceMap.Entries, documents.Documents);
        foreach (var entry in validatedSourceMap.Entries)
        {
            if (!index.Ids.Contains(entry.SemanticId))
            {
                throw new InvalidSemanticContract($"Source map identity '{entry.SemanticId}' does not exist in the executable semantic model.");
            }

            if (entry.Origin != index.ResolveOrigin(entry.SemanticId, documents.IdentityCatalog))
            {
                throw new InvalidSemanticContract($"Source map identity '{entry.SemanticId}' disagrees with its effective catalog assignment origin.");
            }
        }

        SemanticModelProvenance.Record(model, documents.IdentityCatalog.Application, index.Declarations);
        return new(model, documents, validatedSourceMap);
    }

    internal SemanticCompilation WithSpecificationOrigins(ImmutableDictionary<SemanticId, EffectiveSpecification> origins) => Copy(origins: origins);

    internal SemanticCompilation WithEnvironment(EnvironmentRequirements environment) => Copy(environment: environment);

    internal SemanticCompilation WithPassiveReadModels(ImmutableHashSet<SemanticId> passiveReadModels) => Copy(passiveReadModels: passiveReadModels);

    internal SemanticCompilation WithSinks(ImmutableArray<SemanticSink> sinks, ImmutableDictionary<SemanticId, string> readModelSinks, ImmutableDictionary<string, string> graphSinks) =>
        Copy(sinks: sinks, readModelSinks: readModelSinks, graphSinks: graphSinks);

    /// <summary>
    /// Copies every realization sidecar onto a new compilation, replacing only the ones given.
    /// </summary>
    internal SemanticCompilation WithSidecarsOf(SemanticCompilation other) =>
        Copy(other.SpecificationOrigins, other.Environment, other.PassiveReadModels, other.Sinks, other.ReadModelSinks, other.GraphSinks);

    SemanticCompilation Copy(
        ImmutableDictionary<SemanticId, EffectiveSpecification>? origins = null,
        EnvironmentRequirements? environment = null,
        ImmutableHashSet<SemanticId>? passiveReadModels = null,
        ImmutableArray<SemanticSink>? sinks = null,
        ImmutableDictionary<SemanticId, string>? readModelSinks = null,
        ImmutableDictionary<string, string>? graphSinks = null) =>
        new(Model, Documents, SourceMap)
        {
            SpecificationOrigins = origins ?? SpecificationOrigins,
            Environment = environment ?? Environment,
            PassiveReadModels = passiveReadModels ?? PassiveReadModels,
            Sinks = sinks ?? Sinks,
            ReadModelSinks = readModelSinks ?? ReadModelSinks,
            GraphSinks = graphSinks ?? GraphSinks
        };
}

sealed class SemanticCompilationIndex
{
    readonly Dictionary<SemanticAddress, SemanticId> _declarations = [];
    readonly Dictionary<SemanticAddress, SemanticEventContract> _events = [];
    readonly HashSet<SemanticId> _ids = [];
    readonly ApplicationIdentity _application;

    SemanticCompilationIndex(ApplicationIdentity application) => _application = application;

    internal IReadOnlyDictionary<SemanticAddress, SemanticId> Declarations => _declarations;

    internal IReadOnlyDictionary<SemanticAddress, SemanticEventContract> Events => _events;

    internal IReadOnlySet<SemanticId> Ids => _ids;

    internal static SemanticCompilationIndex Create(SemanticApplication application, ApplicationIdentity applicationIdentity)
    {
        if (application is null || !applicationIdentity.IsSet)
        {
            throw new InvalidSemanticContract("A semantic compilation index requires application identities.");
        }

        var index = new SemanticCompilationIndex(applicationIdentity);
        index.Register(SemanticAddress.ForApplication(applicationIdentity), application.Id);
        foreach (var concept in application.Concepts)
        {
            index.Register(SemanticAddress.ForConcept(applicationIdentity, concept.Name), concept.Id);
        }

        foreach (var type in application.Types)
        {
            var typeAddress = SemanticAddress.ForCompositeType(applicationIdentity, type.Name);
            index.Register(typeAddress, type.Id);
            index.RegisterProperties(typeAddress, type.Properties);
        }

        foreach (var trigger in application.Triggers)
        {
            var triggerAddress = SemanticAddress.ForTrigger(applicationIdentity, trigger.Name);
            index.Register(triggerAddress, trigger.Id);
            index.RegisterProperties(triggerAddress, trigger.Properties);
        }

        foreach (var source in application.EventSources)
        {
            var sourceAddress = SemanticAddress.ForEventSource(applicationIdentity, source.Name);
            index.Register(sourceAddress, source.Id);
            foreach (var stream in source.Streams)
            {
                index.Register(SemanticAddress.ForEventStream(sourceAddress, stream.Name), stream.Id);
            }
        }

        foreach (var module in application.Modules)
        {
            index.RegisterModule(module);
        }

        return index;
    }

    internal SemanticIdentityOrigin ResolveOrigin(SemanticId id, SemanticIdentityCatalog catalog)
    {
        var address = _declarations.Single(_ => _.Value == id).Key;
        return _events.ContainsKey(address)
            ? catalog.ResolveEventContract(address).Origin
            : catalog.ResolveSemanticAssignment(address).Origin;
    }

    void RegisterModule(SemanticModule module)
    {
        Register(SemanticAddress.ForModule(_application, module.Name), module.Id);
        foreach (var feature in module.Features)
        {
            RegisterFeature(module.Name, [], feature);
        }
    }

    void RegisterFeature(string module, ImmutableArray<string> parentPath, SemanticFeature feature)
    {
        var path = parentPath.Add(feature.Name);
        Register(SemanticAddress.ForFeature(_application, module, path), feature.Id);
        foreach (var nested in feature.Features)
        {
            RegisterFeature(module, path, nested);
        }

        foreach (var slice in feature.Slices)
        {
            RegisterSlice(module, path, slice);
        }
    }

    void RegisterSlice(string module, ImmutableArray<string> featurePath, SemanticSlice slice)
    {
        var sliceAddress = SemanticAddress.ForSlice(_application, module, featurePath, slice.Name);
        Register(sliceAddress, slice.Id);
        foreach (var eventContract in slice.Events)
        {
            var eventAddress = SemanticAddress.ForEventContract(sliceAddress, eventContract.Name);
            Register(eventAddress, eventContract.Id);
            if (eventContract.PriorRevisions.IsEmpty)
            {
                RegisterProperties(eventAddress, eventContract.Properties);
            }
            else
            {
                foreach (var prior in eventContract.PriorRevisions)
                {
                    RegisterEventProperties(eventAddress, prior.Revision, prior.Properties);
                }

                RegisterEventProperties(eventAddress, eventContract.Revision, eventContract.Properties);
            }

            _events.Add(eventAddress, eventContract);
        }

        foreach (var command in slice.Commands)
        {
            var commandAddress = SemanticAddress.ForCommand(sliceAddress, command.Name);
            Register(commandAddress, command.Id);
            RegisterProperties(commandAddress, command.Properties);
        }

        foreach (var readModel in slice.ReadModels)
        {
            var readModelAddress = SemanticAddress.ForReadModel(sliceAddress, readModel.Name);
            Register(readModelAddress, readModel.Id);
            RegisterProperties(readModelAddress, readModel.Properties);
        }

        foreach (var projection in slice.Projections)
        {
            Register(SemanticAddress.ForProjection(sliceAddress, projection.Name), projection.Id);
        }

        foreach (var query in slice.Queries)
        {
            var queryAddress = SemanticAddress.ForQuery(sliceAddress, query.Name);
            Register(queryAddress, query.Id);
            if (query.Argument is not null)
            {
                Register(SemanticAddress.ForQueryArgument(queryAddress, query.Argument.Name), query.Argument.Id);
            }
            foreach (var part in query.KeyParts)
            {
                Register(SemanticAddress.ForQueryArgument(queryAddress, part.Name), part.Id);
            }
            foreach (var filter in query.Filters)
            {
                Register(SemanticAddress.ForQueryArgument(queryAddress, filter.Name), filter.Id);
            }
        }

        foreach (var specification in slice.Specifications)
        {
            Register(SemanticAddress.ForSpecification(sliceAddress, specification.Name), specification.Id);
        }

        foreach (var reaction in slice.Reactions)
        {
            Register(SemanticAddress.ForReaction(sliceAddress, reaction.Name), reaction.Id);
        }

        foreach (var capture in slice.Captures)
        {
            Register(SemanticAddress.ForCapture(sliceAddress, capture.Name), capture.Id);
        }

        foreach (var screen in slice.Screens)
        {
            Register(SemanticAddress.ForScreen(sliceAddress, screen.Name), screen.Id);
        }
    }

    void RegisterEventProperties(SemanticAddress owner, EventContractRevision revision, ImmutableArray<SemanticProperty> properties)
    {
        foreach (var property in properties)
        {
            Register(SemanticAddress.ForEventProperty(owner, revision, property.Name), property.Id);
        }
    }

    void RegisterProperties(SemanticAddress owner, ImmutableArray<SemanticProperty> properties)
    {
        foreach (var property in properties)
        {
            Register(SemanticAddress.ForProperty(owner, property.Name), property.Id);
        }
    }

    void Register(SemanticAddress address, SemanticId id)
    {
        if (!_declarations.TryAdd(address, id) || !_ids.Add(id))
        {
            throw new InvalidSemanticContract("A semantic compilation contains a duplicate declaration address or identity.");
        }
    }
}
