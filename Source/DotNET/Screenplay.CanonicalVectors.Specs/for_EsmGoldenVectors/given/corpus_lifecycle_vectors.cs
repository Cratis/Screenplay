// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors.given;

public class corpus_lifecycle_vectors : Specification
{
    protected (string Id, ImmutableArray<byte> Bytes)[] _vectors;

    void Establish()
    {
        var properties = typeof(CanonicalCorpusVector).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(CanonicalCorpusVector) ||
                property.PropertyType == typeof(ImmutableArray<CanonicalCorpusVector>) ||
                property.PropertyType == typeof(EsmGoldenVector));
        _vectors = [.. properties.SelectMany(Read)
            .Concat(EsmGoldenVectors.Versions.Select(version => ($"EsmGoldenVectors.V{version}", EsmGoldenVectors.Get(version).Bytes)))
            .Concat(EsmGoldenVectors.MinorVersions.Select(version => ($"EsmGoldenVectors.V{version}", EsmGoldenVectors.GetMinor(version).Bytes)))
            .OrderBy(vector => vector.Item1, StringComparer.Ordinal)];
        Assert.Equal(_vectors.Length, _vectors.Select(vector => vector.Id).Distinct(StringComparer.Ordinal).Count());
    }

    protected static IEnumerable<(string Kind, string Owner, string Name)> Specifications(ExecutableSemanticModel model) =>
        model.Application.StateMachines.SelectMany(owner => owner.Specifications.Select(specification => ("state machine", owner.Name, specification.Name)))
            .Concat(model.Application.Workflows.SelectMany(owner => owner.Specifications.Select(specification => ("workflow", owner.Name, specification.Name))));

    static IEnumerable<(string, ImmutableArray<byte>)> Read(PropertyInfo property)
    {
        var id = $"{property.DeclaringType!.Name}.{property.Name}";

        return property.GetValue(null) switch
        {
            CanonicalCorpusVector vector => [(id, vector.EsmBytes)],
            ImmutableArray<CanonicalCorpusVector> vectors => vectors.Select(vector => ($"{id}[{vector.Name}]", vector.EsmBytes)),
            EsmGoldenVector vector => [(id, vector.Bytes)],
            _ => []
        };
    }
}
