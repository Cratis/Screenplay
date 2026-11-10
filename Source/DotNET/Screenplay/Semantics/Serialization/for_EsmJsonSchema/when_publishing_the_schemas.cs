// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema.given;
using Cratis.Screenplay.Semantics.Serialization.given;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema;

public class when_publishing_the_schemas(ITestOutputHelper output) : Specification
{
    void Establish()
    {
        if (Environment.GetEnvironmentVariable(published_esm_schemas.RegenerateVariable) != "1") return;
        Directory.CreateDirectory(published_esm_schemas.Folder);
        foreach (var version in EsmJsonSchema.Versions)
        {
            File.WriteAllText(Path.Combine(published_esm_schemas.Folder, EsmJsonSchema.FileName(version)), EsmJsonSchema.For(version));
        }
    }

    [Fact]
    void should_publish_a_schema_for_every_released_version()
    {
        var expected = EsmSchemaV46Support.SemanticVersions.Where(version => EsmBaselineShapes.Majors.Contains(version.Major)).Select(EsmJsonSchema.FileName).Order(StringComparer.Ordinal).ToArray();
        var published = Directory.EnumerateFiles(published_esm_schemas.Folder, "esm-v*.schema.json").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
        published.ShouldEqual(expected);
        output.WriteLine($"Checked {published.Length} published ESM schemas.");
    }

    [Fact]
    void should_match_the_generator_byte_for_byte()
    {
        foreach (var version in EsmJsonSchema.Versions)
        {
            Assert.True(
                published_esm_schemas.Read(version) == EsmJsonSchema.For(version),
                $"{EsmJsonSchema.FileName(version)} disagrees with the ESM vocabulary. Run the specs with {published_esm_schemas.RegenerateVariable}=1 and review the change; schemas of released versions must not change.");
        }
    }

    [Fact] void should_publish_the_newest_declared_version() => EsmJsonSchema.Versions[^1].ShouldEqual(frozen_esm_goldens.CurrentVersion);
    [Fact] void should_publish_the_released_minor() => EsmJsonSchema.Versions.ShouldContain(SemanticVersion.V37_1);
    [Fact] void should_name_minors_with_their_minor() => EsmJsonSchema.FileName(SemanticVersion.V37_1).ShouldEqual("esm-v37.1.schema.json");
    [Fact] void should_identify_each_schema_by_its_download_address() => EsmJsonSchema.For(SemanticVersion.V41).ShouldContain($"\"$id\": \"{EsmJsonSchema.BaseUrl}esm-v41.schema.json\"");
}
#endif
