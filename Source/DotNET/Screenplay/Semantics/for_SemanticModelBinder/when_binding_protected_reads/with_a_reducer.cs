// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_a_reducer : given.a_protected_read_model
{
    [Fact]
    void should_refuse_a_reducer_view()
    {
        var source = Source.Replace("      projection Build => View\n        from Recorded\n          id = $eventSourceId\n          value = value\n        remove with Removed", "      reducer Reduce => View\n        on Recorded\n          ```csharp\n          return context.State;\n          ```", StringComparison.Ordinal);
        Refusal(Bind(source), SemanticDecisionReadRefusal.Reducer);
    }
}
