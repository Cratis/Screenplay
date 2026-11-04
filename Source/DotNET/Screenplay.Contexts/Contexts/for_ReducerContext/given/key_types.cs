// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ReducerContext.given;

public record UuidKey(Guid Value);
public record TextKey(string Value);
public record PairKey(Guid A, string B);

public record AmbiguousKey
{
    public AmbiguousKey(string value) { }
    public AmbiguousKey(Guid value) { }
}

public record RejectingKey
{
    public RejectingKey(Guid value) => throw new KeyRejected();
}

public class KeyRejected() : Exception("The concept rejected the key.");
