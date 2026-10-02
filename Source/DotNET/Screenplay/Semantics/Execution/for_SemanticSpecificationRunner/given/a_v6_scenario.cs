// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

public class a_v6_scenario : Specification
{
    protected SemanticExecutionPlan _plan;

    protected void Compile(string source)
    {
        const string StableKey = "v6-scenario";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Billing.play", source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics.Where(_ => _.Severity == Diagnostics.DiagnosticSeverity.Error).Select(_ => _.Message)));
        }

        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    protected SemanticSpecificationRun Run(string specification) =>
        new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Values.Single(value => value.Name == specification).Id);
}
