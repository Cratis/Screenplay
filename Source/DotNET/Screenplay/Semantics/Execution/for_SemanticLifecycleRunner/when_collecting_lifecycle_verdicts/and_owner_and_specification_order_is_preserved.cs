// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_collecting_lifecycle_verdicts;

public class and_owner_and_specification_order_is_preserved : given.a_lifecycle_compilation
{
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Establish()
    {
        var workflow = Model.Application.Workflows.Single();
        // Repeated owners isolate traversal order without adding declarations.
        ReplaceApplication(Model.Application with
        {
            StateMachines = [Machine with { Name = "Second", Specifications = [Machine.Specifications[1], Machine.Specifications[0]] }, Machine],
            Workflows = [workflow with { Name = "Other", Specifications = [workflow.Specifications[4]] }, workflow with { Specifications = [workflow.Specifications[0]] }]
        });
    }

    void Because() => _results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_preserve_owner_and_specification_order() => _results.Select(result => $"{result.Owner}.{result.Name}").ToArray().ShouldEqual(new[]
    {
        "Second.AnOverdueInvoiceIsOverdue", "Second.SendingStartsTheCollection",
        "InvoiceLifecycle.SendingStartsTheCollection", "InvoiceLifecycle.AnOverdueInvoiceIsOverdue",
        "Other.AStartedRunFails", "Collect.APaidInvoiceIsTagged"
    });
}
