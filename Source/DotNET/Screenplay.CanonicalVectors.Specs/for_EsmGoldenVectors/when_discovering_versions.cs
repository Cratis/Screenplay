// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.given;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_discovering_versions : given.golden_files
{
    ImmutableArray<int> _versions;

    void Because() => _versions = EsmGoldenVectors.Versions;

    [Fact] void should_pin_all_28_declared_versions_by_a_full_golden_or_feature_corpus() => esm_version_pins.AssertEveryDeclaredVersionIsPinned();
    [Fact] void should_keep_only_legacy_uncovered_and_recent_full_goldens() => _versions.ShouldEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 24, 27, 28 });
    [Fact] void should_match_the_golden_folder() => _versions.ShouldEqual(Directory.GetFiles(_goldenFolder, "full-esm-v*.json").Select(path => int.Parse(Path.GetFileNameWithoutExtension(path)["full-esm-v".Length..], CultureInfo.InvariantCulture)).Order());
}
