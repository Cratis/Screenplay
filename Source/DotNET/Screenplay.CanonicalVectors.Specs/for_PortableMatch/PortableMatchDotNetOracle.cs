// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET10_0
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PortableMatch;

static class PortableMatchDotNetOracle
{
    static PortableMatchDotNetOracle()
    {
        // Test-process oracle allowance, not matcher admission or a generated application's setting.
        // .NET rejected a boundary shape with a 14995-node estimate, above its 10000 default.
        // A bounded 20000 lets the independent engine check the whole portable-budget boundary.
        AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", 20000);
    }

    internal static Func<string, bool> Compile(MatchPatternResult pattern) =>
        new Regex(pattern.NormalizedDotNetPattern, RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout).IsMatch;
}
#endif
