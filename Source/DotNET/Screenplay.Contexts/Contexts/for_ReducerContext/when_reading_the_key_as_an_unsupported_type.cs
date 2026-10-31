// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext;

public class when_reading_the_key_as_an_unsupported_type : Specification
{
    ReducerContext _context;
    Exception _primitiveError;
    Exception _pairError;
    Exception _ambiguousError;

    void Establish() => _context = new(null, 0, "3fa85f64-5717-4562-b3fc-2c963f66afa6", TenantId.Default, DateTimeOffset.UnixEpoch, 0);
    void Because()
    {
        _primitiveError = Catch.Exception(() => _context.KeyAs<int>());
        _pairError = Catch.Exception(() => _context.KeyAs<given.PairKey>());
        _ambiguousError = Catch.Exception(() => _context.KeyAs<given.AmbiguousKey>());
    }

    [Fact] void should_refuse_the_primitive() => _primitiveError.ShouldBeOfExactType<ContextKeyNotConvertible>();
    [Fact] void should_refuse_the_pair() => _pairError.ShouldBeOfExactType<ContextKeyNotConvertible>();
    [Fact] void should_refuse_the_ambiguous_constructors() => _ambiguousError.ShouldBeOfExactType<ContextKeyNotConvertible>();
    [Fact] void should_explain_the_primitive_failure() => _primitiveError.Message.ShouldEqual("Context key cannot be read as System.Int32: it is not string, System.Guid or a type with exactly one public constructor taking a single string or System.Guid.");
    [Fact] void should_explain_the_pair_failure() => _pairError.Message.ShouldEqual($"Context key cannot be read as {typeof(given.PairKey).FullName}: it is not string, System.Guid or a type with exactly one public constructor taking a single string or System.Guid.");
    [Fact] void should_explain_the_ambiguous_failure() => _ambiguousError.Message.ShouldEqual($"Context key cannot be read as {typeof(given.AmbiguousKey).FullName}: it is not string, System.Guid or a type with exactly one public constructor taking a single string or System.Guid.");
}
