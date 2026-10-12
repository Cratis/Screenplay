// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v13 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("13.0").ShouldEqual(LanguageVersion.V13);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("13.0").ShouldEqual(SemanticVersion.V13);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV13Support.Supports(LanguageVersion.V13, SemanticVersion.V13).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV13Support.Supports(LanguageVersion.V12, SemanticVersion.V13).ShouldBeFalse();
    [Fact] void should_include_every_v12_pair() => EsmSchemaV12Support.LanguageVersions.Zip(EsmSchemaV12Support.SemanticVersions).All(pair => EsmSchemaV13Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v13_in_the_frozen_v12_contract() => EsmSchemaV12Support.Supports(LanguageVersion.V13, SemanticVersion.V13).ShouldBeFalse();
}
