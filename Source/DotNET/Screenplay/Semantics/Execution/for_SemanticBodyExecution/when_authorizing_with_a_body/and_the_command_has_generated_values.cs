// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_command_has_generated_values : given.a_policy
{
    void Establish() => Configure("concept Id : Uuid\n" + Source.Replace("name String identifier", "name String identifier\n        receipt Id generated", StringComparison.Ordinal));
    void Because() => Result = Execute();

    [Fact] void should_authorize_before_identity_allocation() => Contexts.Count.ShouldEqual(1);
    [Fact] void should_not_invent_generated_artifact_members() => Contexts.Single().Artifact.Properties.Length.ShouldEqual(1);
    [Fact] void should_keep_the_explicit_subject() => Contexts.Single().Subject.ShouldEqual("First");
    [Fact] void should_then_refuse_missing_generation_fixtures() => ((SemanticUnsupported)Result).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);

}
