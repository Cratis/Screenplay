// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_the_tenant_is_implicit : given.a_query
{
    void Establish() => Compile("query Get => R[]\n  filter tenantId Uuid from $context.tenant");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_accept_the_implicit_tenant() => Findings.ShouldBeEmpty();
}
