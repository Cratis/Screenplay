// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_composite_key_queries_are_admitted : Specification
{
    McpAuthoringReadiness _readiness;

    void Because()
    {
        var application = new ScreenplayCompiler().Parse("""
            module M
              feature F
                slice StateView S
                  readmodel Row
                    first String key
                    second String key
                  query Find => Row optional
                    by
                      first String
                      second String
                  query ForFirst => Row[]
                    filter first String
            """).Value!;
        _readiness = new McpAuthoringReadiness(application);
    }

    [Fact] void should_not_label_composite_declarations_and_optional_query_parts_as_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeFalse();
    [Fact] void should_not_invent_an_execution_refusal_for_the_admitted_key_shape() => _readiness.ModelExecutionReadiness.ShouldBeNull();
}
