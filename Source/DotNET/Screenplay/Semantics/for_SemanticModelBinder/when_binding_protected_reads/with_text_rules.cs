// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_text_rules : given.a_protected_read_model
{
    [Fact]
    void should_select_v25_above_the_released_text_rule_version()
    {
        var result = Bind("concept Label : String\n  validate\n    starts with \"A\"\n" + Source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V25);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V25);
        SemanticModelValidator.UsesTextRules(result.Value.Model.Application).ShouldBeTrue();
    }
}
