// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v17 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("17.0").ShouldEqual(LanguageVersion.V17);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("17.0").ShouldEqual(SemanticVersion.V17);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV17Support.Supports(LanguageVersion.V17, SemanticVersion.V17).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV17Support.Supports(LanguageVersion.V16, SemanticVersion.V17).ShouldBeFalse();
    [Fact] void should_include_every_v16_pair() => EsmSchemaV16Support.LanguageVersions.Zip(EsmSchemaV16Support.SemanticVersions).All(pair => EsmSchemaV17Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v17_in_the_frozen_v16_contract() => EsmSchemaV16Support.Supports(LanguageVersion.V17, SemanticVersion.V17).ShouldBeFalse();
}
