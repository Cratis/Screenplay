// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_by_disagrees_with_the_keyed_property : given.a_query
{
    void Establish() => Compile("query Get => R optional\n  by id String");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_report_the_parameter() => Findings.Single().Code.ShouldEqual(DiagnosticCodes.QueryParameterNotHeldByView);
    [Fact] void should_locate_the_parameter() => Findings.Single().Location.ShouldEqual(Compilation.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single().By!.Location);
}
