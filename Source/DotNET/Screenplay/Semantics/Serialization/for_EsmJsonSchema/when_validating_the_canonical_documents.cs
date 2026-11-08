// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema.given;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_EsmJsonSchema;

public class when_validating_the_canonical_documents(ITestOutputHelper output) : Specification
{
    (string Path, SemanticVersion Version, JsonElement Document)[] _documents = [];
    List<string> _failures = [];

    void Establish() => _documents = [.. published_esm_schemas.Documents()];

    void Because()
    {
        foreach (var (path, version, document) in _documents)
        {
            using var schema = JsonDocument.Parse(EsmJsonSchema.For(version));
            _failures.AddRange(new json_schema_validator(schema.RootElement).Validate(document).Take(5).Select(error => $"{path}: {error}"));
        }

        output.WriteLine($"Validated {_documents.Length} canonical ESM documents across {_documents.Select(document => document.Version).Distinct().Count()} versions.");
    }

    [Fact] void should_find_the_goldens_and_the_corpus() => _documents.Length.ShouldBeGreaterThan(80);
    [Fact] void should_cover_every_published_version() => _documents.Select(document => document.Version).Distinct().Count().ShouldEqual(EsmJsonSchema.Versions.Length);
    [Fact] void should_validate_every_document() => Assert.True(_failures.Count == 0, string.Join('\n', _failures.Take(40)));
}
#endif
