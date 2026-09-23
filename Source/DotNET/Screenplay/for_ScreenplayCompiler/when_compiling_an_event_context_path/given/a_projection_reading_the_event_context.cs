// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path.given;

public class a_projection_reading_the_event_context : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Projection(string expression) =>
        $"""
        projection Activity => ActivityReadModel
          from Happened
            value = {expression}
        """;
}
