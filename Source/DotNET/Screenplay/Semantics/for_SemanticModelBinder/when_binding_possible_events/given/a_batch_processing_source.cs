// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events.given;

public static class a_batch_processing_source
{
    public const string Golden = """
        eventsource Store
          identifier Uuid
          stream Main
        module Billing
          feature Invoices
            slice StateChange ProcessBatch
              event Started
              event Sent
              event Cancelled
              event Completed
              command ProcessBatch
                batchId Uuid identifier
                invoiceIds Uuid[]
                handler
                  may produce
                    Started
                    Sent for batchId
                    Cancelled for one of invoiceIds
                    Completed for batchId
                      stream Store.Main
                      tag "audit"
                  ```csharp
                  result.Add(new Started());
                  result.Add(new Sent());
                  foreach (var id in InvoiceIds) result.Add(id, new Cancelled());
                  result.Add(new Completed());
                  ```
        """;
}
