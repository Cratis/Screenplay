// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.CanonicalVectors.for_CanonicalCorpusDocument;

public class when_reading_invalid_utf8 : Specification
{
    Exception _exception = null!;

    void Because()
    {
        var document = new CanonicalCorpusDocument
        {
            StableKey = "invalid",
            DisplayPath = "invalid.play",
            Bytes = [0xc3, 0x28]
        };

        _exception = Catch.Exception(() => _ = document.Text);
    }

    [Fact] void should_fail_strict_decoding() => _exception.ShouldBeOfExactType<System.Text.DecoderFallbackException>();
}
