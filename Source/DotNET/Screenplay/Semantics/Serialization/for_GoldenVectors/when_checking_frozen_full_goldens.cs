// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Cratis.Screenplay.Semantics.Serialization.given;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_GoldenVectors;

public class when_checking_frozen_full_goldens(ITestOutputHelper output) : Specification
{
    [Fact]
    void should_pin_every_byte_of_every_version_below_current()
    {
        var manifest = frozen_esm_goldens.Sha256;

        // Versions are numbered at admission (0025): v35 (0119) is admitted and its golden is frozen with every version below the current one.
        var expected = EsmSchemaV36Support.SemanticVersions.Select(version => (int)version.Major).Where(version => version < frozen_esm_goldens.CurrentVersion).Order().ToArray();
        expected.ShouldNotBeEmpty();
        manifest.Keys.Order().ShouldEqual(expected);
        foreach (var version in expected)
        {
            var path = Path.Combine(frozen_esm_goldens.Directory(), $"full-esm-v{version}.json");
            Assert.True(File.Exists(path), $"Frozen golden is missing: {path}");
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            Assert.True(hash == manifest[version], $"Frozen golden changed: {path}. Restore its frozen bytes; do not regenerate it or replace its SHA-256 pin.");
        }
        output.WriteLine($"Checked {expected.Length} frozen full ESM goldens (v1..v{expected[^1]}).");
    }
}
#endif
