// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_discovering_versions : given.golden_files
{
    ImmutableArray<int> _versions;

    void Because() => _versions = EsmGoldenVectors.Versions;

    [Fact] void should_include_every_declared_version() => _versions.ShouldEqual(EsmSchemaV31Support.SemanticVersions.Select(version => (int)version.Major));
    [Fact] void should_match_the_golden_folder() => _versions.ShouldEqual(Directory.GetFiles(_goldenFolder, "full-esm-v*.json").Select(path => int.Parse(Path.GetFileNameWithoutExtension(path)["full-esm-v".Length..], CultureInfo.InvariantCulture)).Order());
}
