// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_later_protected_read_closure_has_two_own_failures : given.security_documents
{
    IEnumerable<SemanticExclusion> Readers => _result.Exclusions.Where(value => value.Address is "M/F/Write" or "M/F/Inspect");

    void Establish() => Source(Protected
        .Replace("  feature F\n", "  feature F\n    slice StateView Removed\n      readmodel EarlierView\n        earlierId String\n      query Scoped => EarlierView[]\n        scoped to identity\n", StringComparison.Ordinal)
        .Replace("    slice StateView Lookup\n", "      query EarlierForWrite => EarlierView[]\n    slice StateView Lookup\n", StringComparison.Ordinal)
        .Replace("    slice StateChange Kept\n", "      query EarlierForInspect => EarlierView[]\n    slice StateChange Kept\n", StringComparison.Ordinal));

    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_bind_the_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_exclude_the_first_failure_and_the_protected_set() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Removed", "M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
    [Fact] void should_report_the_first_pass_failure_as_root() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_report_the_later_own_failures_as_dependent() => Readers.ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Dependent);
    [Fact] void should_name_only_the_earlier_exclusion_for_each_reader() => Readers.ShouldEachConformTo(value => value.DependsOn.SequenceEqual(new SemanticExclusionReference[] { new(SemanticExclusionKind.Slice, "M/F/Removed") }));
    [Fact] void should_keep_the_forcing_readers_for_the_view() => _result.Exclusions[2].DependsOn.AsEnumerable().ShouldEqual([new(SemanticExclusionKind.Slice, "M/F/Write"), new(SemanticExclusionKind.Slice, "M/F/Inspect")]);
    [Fact] void should_terminate_every_dependency_path_at_a_root() => _result.Exclusions.ShouldEachConformTo(value => ReachesRoot(value, []));

    bool ReachesRoot(SemanticExclusion exclusion, HashSet<SemanticExclusionReference> visited)
    {
        if (exclusion.Cause == SemanticExclusionCause.Root) return exclusion.DependsOn.IsEmpty;
        if (!visited.Add(new(exclusion.Kind, exclusion.Address))) return false;
        return !exclusion.DependsOn.IsEmpty && exclusion.DependsOn.All(reference =>
            ReachesRoot(_result.Exclusions.Single(value => value.Kind == reference.Kind && value.Address == reference.Address), new(visited)));
    }
}
