// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_Agents.given;

public class an_agent_model : Specification
{
    protected const string Model = """
        policy Staff
          require role "Staff"
        module Sales
          authorize Staff
          feature Invoices
            slice StateView Listing
              readmodel Invoice
                id String
              query All => Invoice[]
            slice StateChange Issuing
              command Issue
                id String identifier
        """;

    protected const string Allowlist = """
        agents
          tokens personal
          query All
            scope read
            description "Find invoices"
          command Sales.Invoices.Issuing.Issue
            scope write
            description "Issue an invoice"
        """;

    protected ScreenplayCompiler Compiler;

    void Establish() => Compiler = new();
}
