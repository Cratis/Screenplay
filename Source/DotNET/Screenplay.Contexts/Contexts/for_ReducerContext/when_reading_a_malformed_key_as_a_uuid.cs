// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_a_malformed_key_as_a_uuid : Specification
{
    ReducerContext _context;
    Exception _primitiveError;
    Exception _conceptError;

    void Establish() => _context = new(null, 0, " 3fa85f64-5717-4562-b3fc-2c963f66afa6", TenantId.Default, DateTimeOffset.UnixEpoch, 0);
    void Because()
    {
        _primitiveError = Catch.Exception(() => _context.KeyAs<Guid>());
        _conceptError = Catch.Exception(() => _context.KeyAs<given.UuidKey>());
    }

    [Fact] void should_refuse_the_primitive() => _primitiveError.ShouldBeOfExactType<ContextKeyNotConvertible>();
    [Fact] void should_refuse_the_concept() => _conceptError.ShouldBeOfExactType<ContextKeyNotConvertible>();
    [Fact] void should_name_the_requested_primitive_without_the_key() => _primitiveError.Message.ShouldEqual("Context key cannot be read as System.Guid: the key is not a UUID in N, D, B or P form.");
    [Fact] void should_name_the_requested_concept_without_the_key() => _conceptError.Message.ShouldEqual($"Context key cannot be read as {typeof(given.UuidKey).FullName}: the key is not a UUID in N, D, B or P form.");
}
