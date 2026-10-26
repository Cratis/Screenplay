// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class reordered_ordered_members : given.compiled_models
{
    void Because()
    {
        var registered = Source.IndexOf("        produces Registered", StringComparison.Ordinal);
        var named = Source.IndexOf("        produces Named", StringComparison.Ordinal);
        var end = Source.IndexOf("      event Registered", StringComparison.Ordinal);
        var reordered = string.Concat(Source[..registered], Source[named..end], Source[registered..named], Source[end..]);
        _comparison = SemanticModelEquivalence.Compare(Compile(Source), Compile(reordered));
    }

    [Fact] void should_not_be_equivalent() => _comparison.IsEquivalent.ShouldBeFalse();
    [Fact] void should_report_differences_only_in_the_command_slice() => _comparison.Differences.All(_ => _.Path.StartsWith("/application/modules[Projects]/features[Registration]/slices[Register]/", StringComparison.Ordinal)).ShouldBeTrue();
}
