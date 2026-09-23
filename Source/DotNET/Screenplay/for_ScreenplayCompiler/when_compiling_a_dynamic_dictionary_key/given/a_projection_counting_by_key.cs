// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_dynamic_dictionary_key.given;

public class a_projection_counting_by_key : for_ScreenplayCompiler.given.a_compiler
{
    protected static string Projection(string target) =>
        $"""
        projection Statistics => StatisticsReadModel
          all
            count {target}
        """;
}
