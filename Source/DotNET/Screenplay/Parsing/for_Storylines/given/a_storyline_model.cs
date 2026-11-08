// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_Storylines.given;

public class a_storyline_model : Specification
{
    protected const string Model =
        """
        concept ItemId : Uuid

        module Returns
          feature Requests
            slice StateChange RequestReturn
              command RequestReturn
                itemId ItemId
                produces ReturnRequested
                  itemId = itemId

              event ReturnRequested
                itemId ItemId

            slice StateView PendingReturns
              readmodel PendingReturn
                itemId ItemId

              projection PendingReturn
                from ReturnRequested
                  key itemId

              query AllPending => PendingReturn[]

          feature Refunds
            slice StateChange RequestReturn
              command IssueRefund
                itemId ItemId
                produces RefundIssued
                  itemId = itemId

              event RefundIssued
                itemId ItemId

        """;
}
