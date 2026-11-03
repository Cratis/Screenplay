// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

internal static class other_application
{
    internal static SemanticAddress Of(SemanticAddress address) => SemanticAddress.FromCanonical(
        address.Kind,
        address.Parts.SetItem(0, SemanticAddressPart.Create(SemanticAddressPartKind.Application, ApplicationIdentity.Create("Billing").ToString())));
}
