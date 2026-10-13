// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_PersistedIdentityStateSerializer.when_deserializing;

public class with_noncanonical_bytes : given.a_persisted_workspace
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => PersistedIdentityStateSerializer.Deserialize(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Bytes) + "\n")));

    [Fact] void should_refuse_the_state() => _error.ShouldBeOfExactType<InvalidPersistedIdentityState>();
    [Fact] void should_explain_the_refusal() => _error.Message.ShouldContain("noncanonical");
}
