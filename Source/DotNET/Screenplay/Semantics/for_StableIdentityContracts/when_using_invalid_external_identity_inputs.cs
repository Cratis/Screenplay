// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_using_invalid_external_identity_inputs : Specification
{
    Exception[] _exceptions;

    void Because()
    {
        var application = ApplicationIdentity.Create("stable-application");
        string[] invalidKeys = [null!, string.Empty, ".", "..", "folder/key", "folder\\key", "C:\\key", "line\nbreak", "malformed\ud800"];
        _exceptions =
        [
            .. invalidKeys.SelectMany(key => new[]
            {
                Catch.Exception(() => SemanticId.Create(SemanticKind.Command, key)),
                Catch.Exception(() => EventContractId.Create(application, key))
            }),
            Catch.Exception(() => SemanticId.Create(SemanticKind.Unknown, "valid-key")),
            Catch.Exception(() => SemanticId.Create((SemanticKind)int.MaxValue, "valid-key")),
            Catch.Exception(() => EventContractId.Create(default, "valid-key"))
        ];
    }

    [Fact] void should_reject_every_input() => _exceptions.All(exception => exception is not null).ShouldBeTrue();
    [Fact] void should_use_the_typed_semantic_contract_error() => _exceptions.All(exception => exception.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
