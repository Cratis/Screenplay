// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths.given;

public class an_ability_mapping : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Model(string source, string target = "Alerts.Send") =>
        $"""
        concept Priority : Enum
          high
        system Alerts
          ability Send
            priority Priority
        module M
          feature F
            slice Automation S
              event Occurred
              reaction R
                when Occurred
                  invokes {target}
                    priority = {source}
        """;
}
