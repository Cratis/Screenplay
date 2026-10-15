// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_concurrency : given.a_protected_read_model
{
    [Fact]
    void should_refuse_the_ambiguous_consistency_boundary()
    {
        var result = Bind(Source.Replace("reads View by id", "reads View by id\n        concurrency\n          eventSource", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.DecisionReadsWithConcurrency);
    }
}
