// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics.given;

public class a_model_with_a_fenced_dependent : Specification
{
    protected Dictionary<string, string> Sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = "module M\n  feature F\n    slice StateChange Add\n      event Added",
        ["consumer.play"] = """
            module N
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    produces Added
                      for id
            FENCED_BLOCK
                    broken
                  command Unrelated
                    unrelatedBroken
            """
    };

    internal bool HasDiagnostic(ScopedDiagnosticResult result, string directive) => result.Diagnostics.Any(diagnostic =>
        diagnostic.Location.Path == "consumer.play" &&
        diagnostic.Location.Line == Array.FindIndex(Sources["consumer.play"].Split('\n'), line => line.Trim() == directive) + 1);
}
