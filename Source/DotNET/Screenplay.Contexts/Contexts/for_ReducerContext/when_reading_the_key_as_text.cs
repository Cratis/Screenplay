// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_key_as_text : Specification
{
    ReducerContext _context;
    string _key;
    string _empty;

    void Establish() => _context = new(null, 0, " a|b ", TenantId.Default, DateTimeOffset.UnixEpoch, 0);
    void Because()
    {
        _key = _context.KeyAs<string>();
        _empty = (_context with { Key = string.Empty }).KeyAs<string>();
    }

    [Fact] void should_preserve_the_text() => _key.ShouldEqual(" a|b ");
    [Fact] void should_leave_the_key_unchanged() => _context.Key.ShouldEqual(" a|b ");
    [Fact] void should_preserve_empty_text() => _empty.ShouldEqual(string.Empty);
}
