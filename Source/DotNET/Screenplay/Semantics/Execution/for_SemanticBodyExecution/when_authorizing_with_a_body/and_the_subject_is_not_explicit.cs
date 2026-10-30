// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_subject_is_not_explicit : given.a_policy
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_derive_a_subject_from_generated_values_or_destinations(bool generated)
    {
        Contexts.Clear();
        Configure(generated
            ? "concept Id : Uuid\n" + Source.Replace("name String identifier", "name String\n        receipt Id generated identifier", StringComparison.Ordinal)
            : Source.Replace("name String identifier", "name String", StringComparison.Ordinal));
        if (generated)
        {
            var property = Plan.Commands.Values.Single().Properties.Single(property => property.IsGenerated);
            Request = Request with { GeneratedValues = [new(property.Id, SemanticValue.Text("11111111-1111-1111-1111-111111111111"))] };
        }
        Execute().ShouldBeOfExactType<SemanticAccepted>();
        Contexts.Single().Subject.ShouldBeNull();
        Contexts.Single().Artifact.Properties.Length.ShouldEqual(1);
    }
}
