// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.given;

static class described_commands
{
    internal const string Source = """
        policy Accountant
          require role "Accountant"
        module Billing
          feature Fees
            slice StateChange ApplyLateFee
              command ApplyLateFee
                invoiceId String identifier
                days Int
                authorize Accountant
                validate
                  days >= 1 message "Invoice must be overdue"
                handler
                  implementation
                    hint "Calculate the fee from the overdue days"
                    hint "Round to the nearest whole amount"
                produces LateFeeApplied
                  for invoiceId
              event LateFeeApplied
                amount Int
              specification Denied
                given caller
                  authenticated
                when ApplyLateFee
                  invoiceId = "invoice-1"
                  days = 3
                then denied
              specification Invalid
                given caller
                  authenticated
                  role "Accountant"
                when ApplyLateFee
                  invoiceId = "invoice-1"
                  days = 0
                then error "Invoice must be overdue"
              specification Body
                given caller
                  authenticated
                  role "Accountant"
                when ApplyLateFee
                  invoiceId = "invoice-1"
                  days = 3
                then LateFeeApplied
                  for "invoice-1"
                  amount = 30
        """;
}
