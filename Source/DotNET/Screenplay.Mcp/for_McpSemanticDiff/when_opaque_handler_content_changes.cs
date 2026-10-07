// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_opaque_handler_content_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string skeleton = "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n";
        const string handler = "        handler\n          ```csharp\n          return 1;\n          ```\n";
        CompareSnapshots(skeleton + handler, skeleton + handler.Replace("return 1;", "return 2;", StringComparison.Ordinal), seed: Create(skeleton));
    }

    [Fact] void should_report_an_opaque_change_without_interpreting_the_code() => Items("members").Single(item => item.GetProperty("kind").GetString() == "Command").GetProperty("changeKind").GetString().ShouldEqual("opaque-changed");
    [Fact] void should_include_before_and_after_hashes() => (Items("members").Single(item => item.GetProperty("kind").GetString() == "Command").GetProperty("beforeHash").GetString() != Items("members").Single(item => item.GetProperty("kind").GetString() == "Command").GetProperty("afterHash").GetString()).ShouldBeTrue();
    [Fact] void should_report_a_known_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
