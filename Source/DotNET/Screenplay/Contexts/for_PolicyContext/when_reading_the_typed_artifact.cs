// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_PolicyContext;

public class when_reading_the_typed_artifact : Specification
{
    PolicyContext _context;
    int _total;

    void Establish() => _context = new(new List<int> { 20, 22 }, "invoice", Identity.NotSet, TenantId.Default, DateTimeOffset.UtcNow);
    void Because() => _total = _context.ArtifactAs<List<int>>().Sum();

    [Fact] void should_bind_linq_to_the_artifact() => _total.ShouldEqual(42);
    [Fact] void should_preserve_the_dynamic_artifact() => ((List<int>)_context.Artifact).ShouldEqual(_context.ArtifactAs<List<int>>());
}
