// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_unsupported_versions : Specification
{
    bool _languageMinor;
    bool _languageMajor;
    bool _semanticMinor;
    bool _semanticMajor;

    void Because()
    {
        _languageMinor = LanguageVersion.TryParse("1.1", out _);
        _languageMajor = LanguageVersion.TryParse("2.0", out _);
        _semanticMinor = SemanticVersion.TryParse("1.1", out _);
        _semanticMajor = SemanticVersion.TryParse("2.0", out _);
    }

    [Fact] void should_reject_unknown_language_minor() => _languageMinor.ShouldBeFalse();
    [Fact] void should_reject_unknown_language_major() => _languageMajor.ShouldBeFalse();
    [Fact] void should_reject_unknown_semantic_minor() => _semanticMinor.ShouldBeFalse();
    [Fact] void should_reject_unknown_semantic_major() => _semanticMajor.ShouldBeFalse();
}
