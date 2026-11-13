// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_Agents;

public class when_resolving_agent_references : given.an_agent_model
{
    const string Other = """
        module Purchasing
          authorize Staff
          feature Bills
            slice StateView Listing
              readmodel Bill
                id String
              query All => Bill[]
        """;

    [Fact]
    void should_not_choose_an_ambiguous_bare_name()
    {
        var result = Compiler.Compile(Model + "\n" + Other + "\n" + Allowlist);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.AmbiguousAgentOperation);
    }

    [Fact]
    void should_resolve_the_qualified_name()
    {
        var result = Compiler.Compile(Model + "\n" + Other + "\n" + Allowlist.Replace("query All", "query Sales.Invoices.Listing.All", StringComparison.Ordinal));
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_include_nested_features_in_resolution()
    {
        const string model = "policy Staff\n  require role \"Staff\"\nmodule Sales\n  authorize Staff\n  feature Finance\n    feature Invoices\n      slice StateView Listing\n        readmodel Invoice\n          id String\n        query All => Invoice[]";
        var result = Compiler.Compile(model + "\nagents\n  tokens personal\n  query Sales.Finance.Invoices.Listing.All\n    scope write\n    description \"Find invoices\"");
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_keep_resolution_case_sensitive()
    {
        var result = Compiler.Compile(Model + "\n" + Allowlist.Replace("query All", "query all", StringComparison.Ordinal));
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnknownAgentOperation);
    }
}
