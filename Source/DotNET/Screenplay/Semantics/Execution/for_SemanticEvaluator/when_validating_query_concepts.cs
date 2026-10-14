// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_validating_query_concepts : Specification
{
    const string Source = """
        concept SearchText : String
          validate
            min 2 message "Search text is too short"
            min 3 message "Search text needs three characters"
        module Items
          feature Browsing
            slice StateView Browse
              readmodel Item
                itemId SearchText
                status SearchText
              query Lookup => Item optional
                by itemId SearchText
              query Filtered => Item[]
                filter status SearchText optional
        """;
    SemanticExecutionPlan _plan;

    void Establish() => _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(Source).Model).Plan!;

    [Fact] void should_reject_an_out_of_rule_key() => ((SemanticRejected)Execute("Lookup", "a")).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_preserve_the_first_message() => ((SemanticRejected)Execute("Lookup", "a")).Details.ShouldEqual("Search text is too short");
    [Fact] void should_retain_every_failure_in_rule_order() => ((SemanticRejected)Execute("Lookup", "a")).ValidationFailures.Select(failure => failure.Message).ShouldEqual(new[] { "Search text is too short", "Search text needs three characters" });
    [Fact] void should_answer_no_instance_for_a_valid_absent_key() => ((SemanticAccepted)Execute("Lookup", "missing")).Queries.Single().Results.ShouldBeEmpty();
    [Fact] void should_not_validate_an_omitted_optional_filter() => Execute("Filtered", null).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
    [Fact] void should_reject_a_supplied_filter() => ((SemanticRejected)Execute("Filtered", null, "a")).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_leave_shape_errors_as_contract_rejections() => ((SemanticRejected)Execute("Lookup", null)).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_apply_in_an_older_version() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);

    [Fact]
    void should_validate_keys_in_v1_without_promoting_the_model()
    {
        var source = Source.Replace("      query Filtered => Item[]\n        filter status SearchText optional", string.Empty, StringComparison.Ordinal);
        _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(source).Model).Plan!;
        _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        ((SemanticRejected)Execute("Lookup", "a")).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    }

    [Fact]
    void should_authorize_before_validating_arguments()
    {
        var source = "policy Member\n  require authenticated\n" + Source.Replace("by itemId SearchText", "by itemId SearchText\n        authorize Member", StringComparison.Ordinal);
        _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(source).Model).Plan!;
        ((SemanticRejected)Execute("Lookup", "a")).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }

    [Fact]
    void should_preserve_a_string_key_message()
    {
        _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(Source.Replace("\"Search text is too short\"", "$strings.validation.searchText.short", StringComparison.Ordinal)).Model).Plan!;
        var rejected = (SemanticRejected)Execute("Lookup", "a");
        rejected.MessageIsStringKey.ShouldBeTrue();
        rejected.Details.ShouldEqual("$strings.validation.searchText.short");
    }

    [Fact]
    void should_refuse_opaque_argument_rules_with_the_query_capability()
    {
        var source = Source.Replace("min 2 message \"Search text is too short\"", "rule BeSearchable\n      ```csharp\n      return true;\n      ```", StringComparison.Ordinal);
        _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(source).Model).Plan!;
        ((SemanticUnsupported)Execute("Lookup", "a")).Capability.ShouldEqual(SemanticExecutionCapability.Query);
    }

    [Fact]
    void should_not_refuse_an_omitted_filter_with_an_opaque_rule()
    {
        var source = Source.Replace("min 2 message \"Search text is too short\"", "rule BeSearchable\n      ```csharp\n      return true;\n      ```", StringComparison.Ordinal);
        _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(source).Model).Plan!;
        Execute("Filtered", null).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
    }

    SemanticExecutionResult Execute(string name, string? key, string? filter = null)
    {
        var query = _plan.Queries.Values.Single(query => query.Name == name);
        var request = new SemanticQueryRequest(query.Id, key is null ? SemanticValue.Null : SemanticValue.Text(key))
        {
            Filters = filter is null ? [] : [new(query.Filters.Single().Id, SemanticValue.Text(filter))]
        };
        return new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty,
            new SemanticExecutionRequest(default, [], [request], ImmutableDictionary<SemanticId, SemanticValue>.Empty) { IsReadOnly = true });
    }
}
