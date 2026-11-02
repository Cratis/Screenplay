// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    // The application calendar and clock operands are a candidate until they are admitted after ESM 35 publishes;
    // a graph that uses them is never written, so no published byte can silently omit them.
    static void RefuseUnpublishedClock(SemanticApplication application)
    {
        if (SemanticClockUsage.Uses(application))
        {
            throw new InvalidSemanticContract("The application calendar and clock operands canonical serialization is not yet admitted.");
        }
    }
}
