// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_RuleContext;

public class when_reading_typed_payloads : Specification
{
    RuleContext _context;
    int _total;
    int _value;

    void Establish() => _context = new(new List<int> { 20, 22 }, 42, "total", TenantId.Default, CausedBy.NotSet, DateTimeOffset.UtcNow);
    void Because()
    {
        _total = _context.ArtifactAs<List<int>>().Sum();
        _value = _context.ValueAs<int>();
    }

    [Fact] void should_bind_linq_to_the_artifact() => _total.ShouldEqual(42);
    [Fact] void should_read_the_typed_value() => _value.ShouldEqual(42);
    [Fact] void should_preserve_the_dynamic_members() => ((int)_context.Value).ShouldEqual((int)_context.Artifact[0] + (int)_context.Artifact[1]);
}
