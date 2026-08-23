// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

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
        }

        return new(model, documents, validatedSourceMap);
    }
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

        foreach (var module in application.Modules)
        {
            index.RegisterModule(module);
        }

        return index;
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
            RegisterProperties(eventAddress, eventContract.Properties);
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
            Register(SemanticAddress.ForQuery(sliceAddress, query.Name), query.Id);
            RegisterUnaddressed(query.Argument.Id);
        }

        foreach (var specification in slice.Specifications)
        {
            Register(SemanticAddress.ForSpecification(sliceAddress, specification.Name), specification.Id);
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

    void RegisterUnaddressed(SemanticId id)
    {
        if (!_ids.Add(id))
        {
            throw new InvalidSemanticContract($"Semantic identity '{id}' is duplicated in the compilation.");
        }
    }
}
