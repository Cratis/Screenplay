// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v18 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("18.0").ShouldEqual(LanguageVersion.V18);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("18.0").ShouldEqual(SemanticVersion.V18);
    [Fact] void should_support_the_v18_pair() => EsmSchemaV18Support.Supports(LanguageVersion.V18, SemanticVersion.V18).ShouldBeTrue();
    [Fact] void should_keep_v17_support_frozen() => EsmSchemaV17Support.Supports(LanguageVersion.V18, SemanticVersion.V18).ShouldBeFalse();
    [Fact] void should_not_support_mixed_version_pairs() => EsmSchemaV18Support.Supports(LanguageVersion.V17, SemanticVersion.V18).ShouldBeFalse();
}
