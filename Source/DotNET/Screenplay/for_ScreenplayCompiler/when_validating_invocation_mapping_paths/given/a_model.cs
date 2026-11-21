// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths.given;

public class a_model : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Model(string source, string target = "note", string trigger = "Occurred") =>
        $"""
        type Detail
          note String
        trigger External
          note String
          detail Detail
          figures Detail[]
          opaque
        module M
          feature F
            slice Automation S
              event Occurred
                note String
                detail Detail
                figures Detail[]
              command Record
                note String
                detail Detail
                figures Detail[]
              reaction R
                when {trigger}
                  invokes Record
                    {target} = {source}
        """;
}
