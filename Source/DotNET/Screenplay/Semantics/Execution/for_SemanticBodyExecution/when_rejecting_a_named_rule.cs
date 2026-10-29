// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_rejecting_a_named_rule : given.a_rule
{
    void Establish() => Answer = new(false);
    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_keep_the_authored_message() => ((SemanticRejected)Result).Details.ShouldEqual("A name is required");
    [Fact] void should_report_validation() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_not_publish_facts() => Result.World.ShouldEqual(SemanticWorld.Empty);
}
