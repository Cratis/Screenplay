// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_collection_is_queried_by_a_view_field : given.a_query
{
    void Establish() => Compile("query List => R[]\n  by name String");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_the_named_field_despite_a_different_projection_key() => Findings.ShouldBeEmpty();
}
