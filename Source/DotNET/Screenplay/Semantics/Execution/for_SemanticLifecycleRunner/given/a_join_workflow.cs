// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.given;

public class a_join_workflow : Specification
{
    protected ExecutableSemanticModel Model = null!;
    protected ImmutableArray<SemanticLifecycleSpecificationResult> Results;

    protected void BindWorkflow(string nodes, string expectations, int items = 2)
    {
        var preamble = a_lifecycle_source.Source[..a_lifecycle_source.Source.IndexOf("workflow Collect", StringComparison.Ordinal)];
        var lines = string.Join('\n', Enumerable.Range(0, items).Select(index => $"        item\n          sku = \"{index}\""));
        var source = preamble + "workflow Collect\n  for Invoice\n" + nodes + "\n" +
            "  specification JoinExpectation\n    given InvoiceRegistered\n      for \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n      lines\n" + lines +
            "\n    when start \"9c858901-8a57-4791-81fe-4c455b099bc9\"\n" + expectations + "\n";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var result = new SemanticModelBinder(2026).Bind("Billing", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        Model = result.Value!.Model;
    }
}
