// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_the_keyed_query_identifies_a_field : given.a_view
{
    void Establish() => Compile("slice StateView View\n  event Changed\n    sourceId Uuid\n  readmodel R\n    id Uuid\n  query Get => R optional\n    by id Uuid\n  projection P => R\n    no automap\n    from Changed key sourceId");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_the_structurally_identified_field() => Findings.ShouldBeEmpty();
}
