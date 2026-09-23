// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_GoldenVectors;

// With SCREENPLAY_REGENERATE_GOLDEN=1 this rewrites the golden files and fails with GoldenVectorsRegenerated;
// without it, it proves the run compares against the checked-in bytes and never rewrites them.
public class when_checking_for_a_regeneration_request : Specification
{
    void Because() => golden_vector_regeneration.RegenerateWhenRequested();

    [Fact] void should_compare_against_the_checked_in_vectors_without_rewriting_them() => golden_vector_regeneration.IsRequested.ShouldBeFalse();
}
#endif
