// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_PersistedIdentityStateSerializer.when_deserializing;

public class with_corrupt_bytes : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => PersistedIdentityStateSerializer.Deserialize(Encoding.UTF8.GetBytes("<<<<<<< main")));

    [Fact] void should_refuse_the_state() => _error.ShouldBeOfExactType<InvalidPersistedIdentityState>();
}
