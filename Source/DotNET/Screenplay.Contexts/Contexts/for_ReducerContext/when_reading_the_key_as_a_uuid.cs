// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_key_as_a_uuid : Specification
{
    ReducerContext _context;
    Guid _key;
    given.UuidKey _concept;

    void Establish() => _context = new(null, 0, "3FA85F64-5717-4562-B3FC-2C963F66AFA6", TenantId.Default, DateTimeOffset.UnixEpoch, 0);
    void Because()
    {
        _key = _context.KeyAs<Guid>();
        _concept = _context.KeyAs<given.UuidKey>();
    }

    [Fact] void should_parse_the_uuid() => _key.ShouldEqual(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
    [Fact] void should_construct_the_concept() => _concept.ShouldEqual(new given.UuidKey(_key));
}
