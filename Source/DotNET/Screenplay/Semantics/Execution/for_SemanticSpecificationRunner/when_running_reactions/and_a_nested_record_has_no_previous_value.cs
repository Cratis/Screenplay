// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_nested_record_has_no_previous_value : given.a_record_capture
{
    [Fact]
    void should_only_add_the_nested_record_for_missing_or_null_previous_values()
    {
        foreach (var previous in new[] { "", "contact = null" })
        {
            CompileScenario($$"""
                      specification NewContact
                        given capture Records
                          id = "root"
                          sourceStatus = "sendt"
                          {{previous}}
                        when capture Records
                          id = "root"
                          sourceStatus = "sendt"
                          contact = {"value":"after","label":"new"}
                        then NestedAdded
                """);
            var run = Run("NewContact");
            run.Passed.ShouldBeTrue();
            ((SemanticAccepted)run.Execution).Facts.Length.ShouldEqual(1);
        }
    }
}
