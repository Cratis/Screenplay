// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_the_enum_is_stored_through_nested_types : given.an_enumeration
{
    void Establish()
    {
        var source = Model(Positional).Replace("module Projects", "type Inner\n  status Status\ntype Outer\n  inner Inner\nmodule Projects", StringComparison.Ordinal)
            .Replace("        status Status", "        details Outer", StringComparison.Ordinal);
        _before = Create(source);
        Assert.True(_before.Compilation.Success, string.Join("; ", _before.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        ChangeSource(source.Replace(Positional, "paid\n  issued\n  draft", StringComparison.Ordinal).Replace("        details Outer", "        details String", StringComparison.Ordinal));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_both_number_changes() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_report_baseline_storage_usage_through_nested_types() => _result.Enumerations.All(change => change.Stored).ShouldBeTrue();
}
