// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_CommandContext;

public class when_reading_the_typed_command : Specification
{
    CommandContext _context;
    int _total;

    void Establish() => _context = new(new Invoice([new Line(20), new Line(22)]), TenantId.Default, Identity.NotSet, CausedBy.NotSet, Causation.NotSet, DateTimeOffset.UtcNow);
    void Because() => _total = _context.CommandAs<Invoice>().Lines.Sum(line => line.Amount);

    [Fact] void should_bind_linq_to_the_typed_lines() => _total.ShouldEqual(42);
    [Fact] void should_preserve_the_dynamic_command() => ((Invoice)_context.Command).ShouldEqual(_context.CommandAs<Invoice>());

    record Invoice(List<Line> Lines);
    record Line(int Amount);
}
