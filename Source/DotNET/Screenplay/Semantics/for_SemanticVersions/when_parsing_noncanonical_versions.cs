// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_noncanonical_versions : Specification
{
    bool _leadingZero;
    bool _missingMinor;
    bool _zeroMajor;
    LanguageVersion _languageVersion;
    SemanticVersion _semanticVersion;

    void Because()
    {
        _leadingZero = LanguageVersion.TryParse("01.0", out _languageVersion);
        _missingMinor = SemanticVersion.TryParse("1", out _semanticVersion);
        _zeroMajor = SemanticVersion.TryParse("0.1", out _);
    }

    [Fact] void should_reject_leading_zeroes() => _leadingZero.ShouldBeFalse();
    [Fact] void should_reject_a_missing_minor_version() => _missingMinor.ShouldBeFalse();
    [Fact] void should_reject_a_zero_major_version() => _zeroMajor.ShouldBeFalse();
    [Fact] void should_return_a_default_language_version() => _languageVersion.ShouldEqual(default);
    [Fact] void should_return_a_default_semantic_version() => _semanticVersion.ShouldEqual(default);
}
