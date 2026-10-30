// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_projecting_query_contexts : given.a_policy
{
    [Theory]
    [InlineData("unknown-circumstances")]
    [InlineData("by-parts")]
    [InlineData("unkeyed")]
    void should_project_only_known_query_context(string shape)
    {
        Contexts.Clear();
        var source = shape switch
        {
            "by-parts" => Source.Replace("name String key", "name String key\n        region String key", StringComparison.Ordinal).Replace("by name String", "by\n          name String\n          region String", StringComparison.Ordinal),
            "unkeyed" => Source.Replace("=> Order optional", "=> Order[]", StringComparison.Ordinal).Replace("        by name String\n", string.Empty, StringComparison.Ordinal),
            _ => Source
        };
        Configure(source);
        var query = Plan.Queries.Values.Single();
        if (shape == "by-parts")
        {
            var key = SemanticValue.Composite([.. query.KeyParts.Select(part => new SemanticPropertyValue(part.KeyProperty, SemanticValue.Text(part.Name)))]);
            Request = SemanticExecutionRequest.ForQueries([new(query.Id, key)]) with { Caller = Request.Caller };
        }
        else if (shape == "unkeyed") Request = SemanticExecutionRequest.ForQueries([new(query.Id, SemanticValue.Null)]) with { Caller = Request.Caller };
        else Request = QueryRequest() with { Tenant = null, Occurrence = null };
        Execute().ShouldBeOfExactType<SemanticAccepted>();
        var context = Contexts.Single();
        context.Tenant.ShouldBeNull();
        context.Occurred.ShouldBeNull();
        if (shape == "by-parts")
        {
            context.Subject.ShouldBeNull();
            context.Artifact.Properties.Select(property => property.TargetProperty).ShouldEqual(query.KeyParts.Select(part => part.Id));
            context.Artifact.Properties.Select(property => property.Value).ShouldEqual(query.KeyParts.Select(part => SemanticValue.Text(part.Name)));
        }
        else if (shape == "unkeyed")
        {
            context.Subject.ShouldBeNull();
            context.Artifact.Properties.ShouldBeEmpty();
        }
        else context.Subject.ShouldEqual("First");
    }
}
