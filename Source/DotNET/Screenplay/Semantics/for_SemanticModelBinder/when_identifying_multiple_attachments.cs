// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_identifying_multiple_attachments : given.a_semantic_binder
{
    const string Source =
        """
        concept Label : String
          validate csharp
            ```
            return true;
            ```
          validate csharp
            ```
            return false;
            ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              event OrderPlaced
                id Uuid
              event OrderCancelled
                id Uuid
              command PlaceOrder
                first String
                second String
                validate csharp
                  ```
                  return true;
                  ```
                validate csharp
                  ```
                  return false;
                  ```
                validate
                  first rule Check
                    file Validations/First.cs
                  second rule Check
                    file Validations/Second.cs
            slice StateView Orders
              readmodel OrderSummary
                id Uuid
              reducer OrderReducer => OrderSummary
                on OrderPlaced
                  file Reducers/Placed.cs
                on OrderCancelled
                  file Reducers/Cancelled.cs
            slice Automation Notify
              reaction NotifyOrder
                when OrderPlaced
                  file Reactions/Placed.cs
                when OrderCancelled
                  file Reactions/Cancelled.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_assign_a_distinct_id_to_every_attachment()
    {
        var ids = _result.ImplementationRequirements.Select(value => value.RequirementId).ToArray();
        ids.Length.ShouldEqual(10);
        ids.Distinct(StringComparer.Ordinal).Count().ShouldEqual(ids.Length);
    }

    [Fact] void should_locate_duplicate_reducer_events()
    {
        var duplicated = Bind(Source.Replace("on OrderCancelled", "on OrderPlaced", StringComparison.Ordinal));
        duplicated.Diagnostics.Any(value => value.Code == DiagnosticCodes.DuplicateReducerEvent && value.Location.Line > 0).ShouldBeTrue();
    }
}
