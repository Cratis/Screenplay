// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_Agents;

public class when_refusing_queries_without_portable_identity : given.an_agent_model
{
    [Fact]
    void should_refuse_a_performed_query()
    {
        var source = Model.Replace("      query All => Invoice[]", "      query All => Invoice[]\n        performer\n          ```csharp\n          return [];\n          ```", StringComparison.Ordinal);
        ExpectRefusal(source, "performed query");
    }

    [Fact]
    void should_refuse_a_graph_query()
    {
        var source = Model.Replace(
            "      query All => Invoice[]",
            """
                  graph Directory
                    node InvoiceNode
                      id String key
                  query All => Invoice[] graph Directory
                    match invoice:InvoiceNode
                    return
                      id = invoice.id
            """,
            StringComparison.Ordinal);
        ExpectRefusal(source, "graph query");
    }

    void ExpectRefusal(string source, string queryKind)
    {
        Compiler.Compile(source).Success.ShouldBeTrue();
        var result = Compiler.Compile(source + "\n" + Allowlist);
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedAgentQueryKind);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldEqual($"Query 'Sales.Invoices.Listing.All' is a {queryKind} and cannot be exposed to agents yet");
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownAgentOperation);
    }
}
