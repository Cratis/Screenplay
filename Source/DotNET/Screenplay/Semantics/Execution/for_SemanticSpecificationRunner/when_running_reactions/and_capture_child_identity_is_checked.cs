// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_capture_child_identity_is_checked : given.a_v6_scenario
{
    SemanticSpecificationRun _duplicate;
    SemanticSpecificationRun _missing;
    SemanticSpecificationRun _typed;
    SemanticSpecificationRun _reordered;
    SemanticSpecificationRun _isolated;
    SemanticSpecificationRun _nested;
    SemanticSpecificationRun _invalidType;

    void Because()
    {
        _duplicate = Present("[{\"id\":1},{\"id\":1}]", "[]");
        _missing = Present("[{\"name\":\"missing\"}]", "[]");
        _typed = Present("[{\"id\":\"1\"}]", "[{\"id\":1}]");
        _reordered = Present("[{\"id\":2},{\"id\":1}]", "[{\"id\":1},{\"id\":2}]");
        _isolated = Present("[]", "[{\"id\":1}]", "other");
        _nested = Present("[]", "[]");
        _invalidType = Present("\"not a collection\"", "[]");
    }

    [Fact] void should_reject_duplicate_child_keys_without_committing_facts() => ((SemanticRejected)_duplicate.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_reject_missing_child_keys_without_guessing_empty_identity() => ((SemanticRejected)_missing.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_distinguish_numeric_and_text_child_keys() => ((SemanticAccepted)_typed.Execution).Facts.Length.ShouldEqual(3);
    [Fact] void should_not_treat_reordering_as_added_or_removed() => ((SemanticAccepted)_reordered.Execution).Facts.Length.ShouldEqual(1);
    [Fact] void should_not_compare_records_from_another_capture_key() => ((SemanticAccepted)_isolated.Execution).Facts.Length.ShouldEqual(0);
    [Fact] void should_reject_a_scalar_in_place_of_children() => ((SemanticRejected)_invalidType.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_append_when_a_nested_record_disappears() => ((SemanticAccepted)_nested.Execution).Facts.Single().EventContract.ShouldEqual(_plan.Events.Values.Single(value => value.Name == "NestedRemoved").Id);

    SemanticSpecificationRun Present(string current, string previous, string previousKey = "root")
    {
        Compile($$"""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
                    children items identified by id
                      append Added
                        when added
                      append Removed
                        when removed
                    nested contact
                      append NestedRemoved
                        when removed
                  event Added
                  event Removed
                  event NestedRemoved
                  specification PresentRecord
                    given capture Records
                      id = "{{previousKey}}"
                      items = {{previous}}
                      contact = { "name": "before" }
                    when capture Records
                      id = "root"
                      items = {{current}}
                    then NestedRemoved
            """);
        return Run("PresentRecord");
    }
}
