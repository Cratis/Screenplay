// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_QueryKeyCompleteness;

public class when_a_collection_by_has_an_incompatible_field_type : given.a_query
{
    void Establish() => Compile("query List => R[]\n  by name Int");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.QueryKeys]));

    [Fact] void should_report_the_parameter() => Findings.Single().Code.ShouldEqual(DiagnosticCodes.QueryParameterNotHeldByView);
}
