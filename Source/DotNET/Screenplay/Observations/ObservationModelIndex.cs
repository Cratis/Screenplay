// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

sealed class ObservationModelIndex
{
    readonly Dictionary<SemanticId, SemanticAddress> _addresses;
    readonly Dictionary<SemanticId, int> _positions = [];
    readonly Dictionary<SemanticId, SemanticId> _owners = [];

    internal ObservationModelIndex(SemanticCompilation compilation)
    {
        ModelSlices = [.. Slices(compilation.Model.Application)];
        Events = ModelSlices.SelectMany(slice => slice.Events).ToDictionary(value => value.Id);
        _addresses = compilation.Documents.IdentityCatalog.Semantics.ToDictionary(value => value.Id, value => value.Address);
        foreach (var slice in ModelSlices)
        {
            Register(slice.Id, slice.Id);
            foreach (var id in slice.Events.Select(value => value.Id).Concat(slice.Commands.Select(value => value.Id))
                .Concat(slice.ReadModels.Select(value => value.Id)).Concat(slice.Projections.Select(value => value.Id)).Concat(slice.Reactions.Select(value => value.Id)))
            {
                Register(id, slice.Id);
            }
        }
    }

    internal SemanticSlice[] ModelSlices { get; }

    internal Dictionary<SemanticId, SemanticEventContract> Events { get; }

    internal static IEnumerable<SemanticSlice> Slices(SemanticApplication application) => application.Modules.SelectMany(module => module.Features.SelectMany(FeatureSlices));

    internal SemanticAddress? Address(SemanticId id) => _addresses.GetValueOrDefault(id);

    internal SemanticId? Owner(SemanticId id) => _owners.TryGetValue(id, out var owner) ? owner : null;

    internal int Position(SemanticId? id) => id is { } value && _positions.TryGetValue(value, out var position) ? position : int.MaxValue;

    static IEnumerable<SemanticSlice> FeatureSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(FeatureSlices));

    void Register(SemanticId id, SemanticId slice)
    {
        _positions[id] = _positions.Count;
        _owners[id] = slice;
    }
}
