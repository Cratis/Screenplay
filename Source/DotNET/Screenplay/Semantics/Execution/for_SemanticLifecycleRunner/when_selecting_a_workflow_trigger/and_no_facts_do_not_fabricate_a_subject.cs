// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_selecting_a_workflow_trigger;

public class and_no_facts_do_not_fabricate_a_subject : given.a_lifecycle_compilation
{
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Establish() => Bind(a_lifecycle_source.Source[..a_lifecycle_source.Source.IndexOf("workflow Collect", StringComparison.Ordinal)] +
        """
        workflow Collect
          for Invoice
          trigger Started manual
            -> Choice
          decide Choice when status == "paid"
            "yes" -> Tag
            "no" -> Charge
          action Tag invokes TagInvoice
          action Charge invokes ChargeFee
          specification EmptyStart
            when start "9c858901-8a57-4791-81fe-4c455b099bc9"
            then invokes ChargeFee
            then completes
        """);

    void Because() => _results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_take_the_false_branch_without_a_subject() => _results.Single(result => result.Name == "EmptyStart").Failures.ShouldBeEmpty();
}
