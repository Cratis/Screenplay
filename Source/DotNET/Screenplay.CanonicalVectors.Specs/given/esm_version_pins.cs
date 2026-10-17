// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.given;

public static class esm_version_pins
{
    public static T[] Declared<T>() => [.. typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(T))
        .Select(field => (T)field.GetValue(null)!)
        .OrderBy(version => SemanticVersion.Parse(version!.ToString()!).Major)
        .ThenBy(version => SemanticVersion.Parse(version!.ToString()!).Minor)];

    public static void AssertEveryDeclaredVersionIsPinned()
    {
        var versions = Declared<SemanticVersion>();
        versions.Length.ShouldEqual(28);
        versions.ShouldEqual(EsmSchemaV28Support.SemanticVersions);
        foreach (var version in versions)
        {
            SemanticModelSerializer.Deserialize(Read(version)).SemanticVersion.ShouldEqual(version);
        }
    }

    public static byte[] Read(SemanticVersion version)
    {
        var major = (int)version.Major;
        if (EsmGoldenVectors.Versions.Contains(major)) return [.. EsmGoldenVectors.Get(major).Bytes];
        var assembly = typeof(EsmGoldenVectors).Assembly;
        var suffix = $".expected.esm-v{major.ToString(CultureInfo.InvariantCulture)}.json";
        var resource = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("Cratis.Screenplay.CanonicalCorpus.Corpus.", StringComparison.Ordinal) && name.EndsWith(suffix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).FirstOrDefault();
        resource.ShouldNotBeNull();
        using var stream = assembly.GetManifestResourceStream(resource!)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }
}
