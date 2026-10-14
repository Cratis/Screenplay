// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v19 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("19.0").ShouldEqual(LanguageVersion.V19);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("19.0").ShouldEqual(SemanticVersion.V19);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV19Support.Supports(LanguageVersion.V19, SemanticVersion.V19).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV19Support.Supports(LanguageVersion.V17, SemanticVersion.V19).ShouldBeFalse();
    [Fact] void should_include_every_v17_pair() => EsmSchemaV17Support.LanguageVersions.Zip(EsmSchemaV17Support.SemanticVersions).All(pair => EsmSchemaV19Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v19_in_the_frozen_v17_contract() => EsmSchemaV17Support.Supports(LanguageVersion.V19, SemanticVersion.V19).ShouldBeFalse();
}
