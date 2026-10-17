// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_single_result_by_is_a_non_identity_field : given.a_query
{
    void Establish() => Compile("query GetByName => R optional\n  by name String", extraQueries: "query GetById => R optional\n  by id Uuid");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_report_the_non_identity_parameter() => Findings.Single().Code.ShouldEqual(DiagnosticCodes.QueryParameterNotHeldByView);
    [Fact] void should_locate_the_non_identity_parameter() => Findings.Single().Location.ShouldEqual(Compilation.Value!.Modules.Single().Features.Single().Slices.Single().Queries.First().By!.Location);
}
