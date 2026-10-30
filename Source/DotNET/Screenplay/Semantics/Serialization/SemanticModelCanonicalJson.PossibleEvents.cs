// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void RefuseUnpublishedHandler(SemanticCommandHandler? handler, SemanticVersion version)
    {
        if (handler is null) return;
        if (handler.ResultVersion == 1 && handler.PossibleEvents.IsDefaultOrEmpty) return;
        if (version.Major < 35) throw new InvalidSemanticContract("Handler possible events and result contract v2 require ESM v35 or later.");
        throw new InvalidSemanticContract("Handler result contract v2 canonical serialization is not yet admitted.");
    }
}
