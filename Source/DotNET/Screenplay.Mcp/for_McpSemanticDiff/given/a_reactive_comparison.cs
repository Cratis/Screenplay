// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff.given;

public class a_reactive_comparison : a_semantic_comparison
{
    internal const string ReactiveSource = """
        trigger Tick
          amount Decimal
        module Projects
          feature Registration
            slice StateChange Decide
              command First
                reason String
              command Second
                reason String
            slice Automation Follow
              reaction Clock
                every 15 minutes
                  invokes First
                    reason = "scheduled"
              reaction Signal
                when Tick
                  amount
                  invokes First
                    reason = "signal"
        """;
}
