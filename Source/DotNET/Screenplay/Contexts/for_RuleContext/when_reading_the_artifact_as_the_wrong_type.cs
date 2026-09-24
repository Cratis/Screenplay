// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_RuleContext;

public class when_reading_the_artifact_as_the_wrong_type : Specification
{
    RuleContext _context;
    Exception _error;

    void Establish() => _context = new("invoice", 42, "total", TenantId.Default, CausedBy.NotSet, DateTimeOffset.UtcNow);
    void Because() => _error = Catch.Exception(() => _context.ArtifactAs<int>());

    [Fact] void should_name_the_artifact_and_types() => _error.Message.ShouldEqual("Context payload 'Artifact' is System.String, not the requested type System.Int32.");
}
