// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_it_is_a_snapshot : given.a_performed_query
{
    SemanticImplementationRequirement[] _requirements;

    void Because() => _requirements = new[] { "CurrentFigures", "LedgerPage", "FiguresInRegion" }.Select(RequirementOf).ToArray();

    [Fact] void should_keep_the_first_result_version() => _requirements.ShouldEachConformTo(_ => _.ResultVersion == 1u);
    [Fact] void should_keep_the_first_context_version() => _requirements.ShouldEachConformTo(_ => _.ContextVersion == 1u);
}
