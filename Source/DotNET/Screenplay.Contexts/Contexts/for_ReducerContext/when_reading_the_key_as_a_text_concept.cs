// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_key_as_a_text_concept : Specification
{
    ReducerContext _context;
    given.TextKey _key;

    void Establish() => _context = new(null, 0, " a|b ", TenantId.Default, DateTimeOffset.UnixEpoch, 0);
    void Because() => _key = _context.KeyAs<given.TextKey>();

    [Fact] void should_wrap_the_text_unchanged() => _key.ShouldEqual(new given.TextKey(" a|b "));
}
