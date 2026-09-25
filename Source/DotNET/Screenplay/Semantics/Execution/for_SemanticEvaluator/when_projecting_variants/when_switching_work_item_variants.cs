// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_variants;

// VariantReclassifier.cs:28-72: entering changes the active read model; global/local From handlers become
// update-only keyed joins and cannot resurrect a departed variant (Decision: 0001).
public class when_switching_work_item_variants : for_SemanticModelBinder.when_binding_variants.a_work_item
{
    const string Identity = "00000000-0000-0000-0000-000000000101";
    ImmutableArray<SemanticReadModelInstance> _before;
    ImmutableArray<SemanticReadModelInstance> _after;
    ImmutableArray<SemanticReadModelInstance> _backfilled;
    string? _beforeFailure;
    string? _afterFailure;
    string? _backfillFailure;
    ImmutableArray<SemanticReadModelInstance> _variantKey;
    string? _variantKeyFailure;

    void Because()
    {
        var model = Bind(Source).Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var events = plan.Events.Values.ToDictionary(@event => @event.Name, StringComparer.Ordinal);
        SemanticFact Fact(string name, params (string Property, SemanticValue Value)[] values) =>
            new(events[name].Id, SemanticValue.Text(Identity), [.. values.Select(value =>
                new SemanticPropertyValue(events[name].Properties.Single(property => property.Name == value.Property).Id, value.Value))]);
        var started = Fact("IssueStarted", ("issueId", SemanticValue.Text(Identity)), ("title", SemanticValue.Text("Initial")));
        var created = Fact("IssueCreated", ("issueId", SemanticValue.Text(Identity)), ("title", SemanticValue.Text("Initial")));
        var title = Fact("TitleChanged", ("issueId", SemanticValue.Text(Identity)), ("title", SemanticValue.Text("Shared")));
        var build = Fact("BuildCompleted", ("issueId", SemanticValue.Text(Identity)), ("buildStatus", SemanticValue.Text("Green")));
        SemanticEvaluator.Establish(plan, [], [created, title, build], out _before, out _beforeFailure);
        SemanticEvaluator.Establish(plan, [], [created, started, title, build], out _after, out _afterFailure);
        SemanticEvaluator.Establish(plan, [], [title, started], out _backfilled, out _backfillFailure);

        // The reference evaluator uses the variant join's explicit issueId key. Chronicle currently uses the
        // event source id instead (Cratis/Chronicle#4165); pin this divergence without changing routing.
        var differentSource = new SemanticFact(title.EventContract, SemanticValue.Text("00000000-0000-0000-0000-000000000202"), title.Values);
        SemanticEvaluator.Establish(plan, [], [created, differentSource], out _variantKey, out _variantKeyFailure);
    }

    [Fact] void should_establish_the_first_variant() => _before.Length.ShouldEqual(1);
    [Fact] void should_keep_global_updates_on_the_active_variant() => _before.Single().Values.Any(value => value.Value == SemanticValue.Text("Shared")).ShouldBeTrue();
    [Fact] void should_not_apply_variant_local_handlers_to_the_other_variant() => _before.Single().Values.All(value => value.Value != SemanticValue.Text("Green")).ShouldBeTrue();
    [Fact] void should_switch_to_the_second_variant_without_resurrecting_the_first() => _after.Length.ShouldEqual(1);
    [Fact] void should_apply_variant_local_handlers_after_entry() => _after.Single().Values.Any(value => value.Value == SemanticValue.Text("Green")).ShouldBeTrue();
    [Fact] void should_backfill_a_shared_event_key_before_entry() =>
        _backfilled.Single().Values.Any(value => value.Value == SemanticValue.Text("Shared")).ShouldBeTrue();
    [Fact] void should_project_without_failure() => (_beforeFailure is null && _afterFailure is null && _backfillFailure is null).ShouldBeTrue();
    [Fact] void should_currently_honor_the_variant_key_instead_of_the_event_source_chronicle_4165() =>
        _variantKey.Single().Values.Any(value => value.Value == SemanticValue.Text("Shared")).ShouldBeTrue();
    [Fact] void should_project_the_variant_key_without_failure() => _variantKeyFailure.ShouldBeNull();
}
