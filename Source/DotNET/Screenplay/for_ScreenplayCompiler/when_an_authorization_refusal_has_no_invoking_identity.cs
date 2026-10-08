// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_an_authorization_refusal_has_no_invoking_identity : given.a_compiler
{
    [Theory]
    [InlineData("module", "authorization", true)]
    [InlineData("feature", "authorization", true)]
    [InlineData("nested", "authorization", true)]
    [InlineData("command", "authorization", true)]
    [InlineData("none", "authorization", false)]
    [InlineData("command", "validation", false)]
    [InlineData("command", "constraint", false)]
    [InlineData("command", "", false)]
    void should_warn_only_for_a_gated_authorization_branch(string gate, string selector, bool expected)
    {
        var source = "policy Access\n  require not role \"Blocked\"\nmodule Billing\n" +
            (gate == "module" ? "  authorize Access\n" : string.Empty) +
            "  feature Payments\n" + (gate == "feature" ? "    authorize Access\n" : string.Empty) +
            "    feature Claims\n" + (gate == "nested" ? "      authorize Access\n" : string.Empty) +
            "      slice Automation Claiming\n        event Approved\n        event Claimed\n        command Claim\n" +
            (gate == "command" ? "          authorize Access\n" : string.Empty) +
            "          produces Claimed\n        reaction Claimer\n          when Approved\n            invokes Claim\n" +
            $"              on refused{(selector.Length > 0 ? $" by {selector}" : string.Empty)}\n                acknowledge\n";
        var result = _compiler.Compile(source);
        var warnings = result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.AuthorizationRefusalWithoutIdentity).ToArray();
        warnings.Length.ShouldEqual(expected ? 1 : 0);
        result.Success.ShouldBeTrue();
        if (!expected) return;
        var warning = warnings.Single();
        warning.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        warning.Location.Path.ShouldBeNull();
        warning.Location.Line.ShouldEqual(source.Split('\n').ToList().FindIndex(line => line.Contains("on refused", StringComparison.Ordinal)) + 1);
        warning.Location.Column.ShouldEqual(15);
        warning.Message.ShouldEqual("Command 'Claim' is authorization-gated, but this invocation has no declared identity. This authorization refusal branch always fires in the reference runner because there is no caller; Arc runs reactor commands as the system. Declare an invoking identity once supported (#383).");
    }
}
