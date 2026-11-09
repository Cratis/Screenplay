// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_remove_key_mapping.given;

public class a_projection_removing_a_key : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Projection(string line) =>
        $"""
        projection Models => ModelReadModel
          from SlotDropped
            {line}
        """;
}
