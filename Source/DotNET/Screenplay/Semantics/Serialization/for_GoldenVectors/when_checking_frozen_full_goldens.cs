// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Cratis.Screenplay.Semantics.Serialization.given;
using Xunit.Abstractions;

namespace Cratis.Screenplay.Semantics.Serialization.for_GoldenVectors;

public class when_checking_frozen_full_goldens(ITestOutputHelper output) : Specification
{
    [Fact]
    void should_pin_every_byte_of_every_version_below_current()
    {
        var manifest = frozen_esm_goldens.Sha256;
        var expected = Enumerable.Range(1, frozen_esm_goldens.CurrentVersion - 1).ToArray();
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
