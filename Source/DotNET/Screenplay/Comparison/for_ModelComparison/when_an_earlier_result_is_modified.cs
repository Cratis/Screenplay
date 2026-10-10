// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_an_earlier_result_is_modified : given.two_models
{
    const string OriginalLimit = "Structural authoring comparison, not an equivalence or execution verdict.";
    IList<string>? _earlierLimits;

    void Establish()
    {
        var earlier = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));
        _earlierLimits = earlier.NotCompared as IList<string>;
        if (_earlierLimits is { IsReadOnly: false }) _earlierLimits[0] = "Caller-local classification";
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    void Destroy()
    {
        if (_earlierLimits is { IsReadOnly: false }) _earlierLimits[0] = OriginalLimit;
    }

    [Fact] void should_preserve_limits_for_later_comparisons() => _result.NotCompared.ShouldContain(OriginalLimit);
}
