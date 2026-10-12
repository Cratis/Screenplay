// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v11 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("11.0").ShouldEqual(LanguageVersion.V11);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("11.0").ShouldEqual(SemanticVersion.V11);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV11Support.Supports(LanguageVersion.V11, SemanticVersion.V11).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV11Support.Supports(LanguageVersion.V10, SemanticVersion.V11).ShouldBeFalse();
    [Fact] void should_include_every_v10_pair() => EsmSchemaV10Support.LanguageVersions.Zip(EsmSchemaV10Support.SemanticVersions).All(pair => EsmSchemaV11Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v11_in_the_frozen_v10_contract() => EsmSchemaV10Support.Supports(LanguageVersion.V11, SemanticVersion.V11).ShouldBeFalse();
}
