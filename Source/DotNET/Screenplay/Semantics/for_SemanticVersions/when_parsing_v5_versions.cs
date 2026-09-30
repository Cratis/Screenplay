// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions;

public class when_parsing_v5_versions : Specification
{
    [Fact] void should_admit_language_v5() => LanguageVersion.Parse("5.0").ShouldEqual(LanguageVersion.V5);
    [Fact] void should_admit_semantic_v5() => SemanticVersion.Parse("5.0").ShouldEqual(SemanticVersion.V5);
    [Fact] void should_reject_unpaired_versions() => EsmSchemaV5Support.Supports(LanguageVersion.V5, SemanticVersion.V4).ShouldBeFalse();
}
