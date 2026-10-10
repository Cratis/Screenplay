// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v10 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("10.0").ShouldEqual(LanguageVersion.V10);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("10.0").ShouldEqual(SemanticVersion.V10);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV10Support.Supports(LanguageVersion.V10, SemanticVersion.V10).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV10Support.Supports(LanguageVersion.V9, SemanticVersion.V10).ShouldBeFalse();
}
