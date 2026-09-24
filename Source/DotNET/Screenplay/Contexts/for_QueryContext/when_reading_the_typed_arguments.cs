// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_QueryContext;

public class when_reading_the_typed_arguments : Specification
{
    QueryContext _context;
    int _total;

    void Establish() => _context = new(new Arguments([20, 22]), TenantId.Default, Identity.NotSet, CausedBy.NotSet, Causation.NotSet, DateTimeOffset.UtcNow);
    void Because() => _total = _context.ArgumentsAs<Arguments>().Amounts.Sum();

    [Fact] void should_bind_linq_to_the_typed_arguments() => _total.ShouldEqual(42);
    [Fact] void should_preserve_the_dynamic_arguments() => ((Arguments)_context.Arguments).ShouldEqual(_context.ArgumentsAs<Arguments>());

    record Arguments(List<int> Amounts);
}
