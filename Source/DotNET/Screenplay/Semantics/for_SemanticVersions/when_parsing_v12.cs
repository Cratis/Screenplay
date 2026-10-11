// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v12 : Specification
{
    [Fact] void should_parse_the_language_version() => LanguageVersion.Parse("12.0").ShouldEqual(LanguageVersion.V12);
    [Fact] void should_parse_the_semantic_version() => SemanticVersion.Parse("12.0").ShouldEqual(SemanticVersion.V12);
    [Fact] void should_admit_the_exact_pair() => EsmSchemaV12Support.Supports(LanguageVersion.V12, SemanticVersion.V12).ShouldBeTrue();
    [Fact] void should_refuse_a_mixed_pair() => EsmSchemaV12Support.Supports(LanguageVersion.V11, SemanticVersion.V12).ShouldBeFalse();
    [Fact] void should_include_every_v11_pair() => EsmSchemaV11Support.LanguageVersions.Zip(EsmSchemaV11Support.SemanticVersions).All(pair => EsmSchemaV12Support.Supports(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_refuse_v12_in_the_frozen_v11_contract() => EsmSchemaV11Support.Supports(LanguageVersion.V12, SemanticVersion.V12).ShouldBeFalse();
}
