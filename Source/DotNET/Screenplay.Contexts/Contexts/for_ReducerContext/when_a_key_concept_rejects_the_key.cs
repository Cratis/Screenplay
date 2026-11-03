// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_a_key_concept_rejects_the_key : Specification
{
    ReducerContext _context;
    Exception _error;

    void Establish() => _context = new(null, 0, "3fa85f64-5717-4562-b3fc-2c963f66afa6", TenantId.Default, DateTimeOffset.UnixEpoch, 0);
    void Because() => _error = Catch.Exception(() => _context.KeyAs<given.RejectingKey>());

    [Fact] void should_propagate_the_concept_validation_error_without_wrapping() => _error.ShouldBeOfExactType<given.KeyRejected>();
}
