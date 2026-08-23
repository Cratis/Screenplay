// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_using_malformed_unicode_for_identity_inputs : a_valid_semantic_model
{
    Exception[] _exceptions;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var stateChange = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Events.Length > 0);
        var malformedEvent = stateChange.Events.Single() with { Name = "ProjectRegistered\ud800" };
        _exceptions =
        [
            Catch.Exception(() => ApplicationIdentity.Create("Projects\ud800")),
            Catch.Exception(() => SemanticAddressPart.Create(SemanticAddressPartKind.Member, "name\udfff")),
            Catch.Exception(() => SemanticAddress.ForModule(application, "Projects\ud800")),
            Catch.Exception(() => EventContractId.CreateLegacy(application, "ProjectRegistered\udfff")),
            Catch.Exception(() => ExecutableSemanticModel.Create(
                LanguageVersion.V1,
                SemanticVersion.V1,
                ReplaceSlice(stateChange with { Events = [malformedEvent] })))
        ];
    }

    [Fact] void should_reject_every_malformed_identity_input_as_an_invalid_semantic_contract() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
