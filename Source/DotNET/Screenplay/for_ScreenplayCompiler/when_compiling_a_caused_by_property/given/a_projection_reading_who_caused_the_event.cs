// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_caused_by_property.given;

public class a_projection_reading_who_caused_the_event : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Projection(string property) =>
        $"""
        projection Activity => ActivityReadModel
          from Happened
            by = $causedBy.{property}
        """;
}
