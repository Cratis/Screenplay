// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_an_authorization_refusal_has_no_invoking_identity : given.a_compiler
{
    [Theory]
    [InlineData("module", false, true)]
    [InlineData("module", false, false)]
    [InlineData("module", true, true)]
    [InlineData("module", true, false)]
    [InlineData("feature", false, true)]
    [InlineData("feature", false, false)]
    [InlineData("feature", true, true)]
    [InlineData("feature", true, false)]
    [InlineData("nested", false, true)]
    [InlineData("nested", false, false)]
    [InlineData("nested", true, true)]
    [InlineData("nested", true, false)]
    void should_use_the_commands_own_repeated_containers_without_crashing(string container, bool gated, bool withRefusal)
    {
        var otherGate = !gated && withRefusal ? "    authorize Access\n" : string.Empty;
        var commandGate = gated ? "    authorize Access\n" : string.Empty;
        var prefix = container switch
        {
            "module" => "module M\n  feature A\nmodule M\n" + (gated ? "  authorize Access\n" : string.Empty) + "  feature B\n",
            "feature" => "module M\n  feature F\n" + otherGate + "  feature F\n" + commandGate,
            _ => "module M\n  feature F\n" + otherGate + "    feature Other\n  feature F\n" + commandGate + "    feature Claims\n"
        };
        var body = "    slice Automation S\n      event Approved\n      command Claim\n      reaction R\n        when Approved\n          invokes Claim\n" +
            (withRefusal ? "            on refused by authorization\n              acknowledge\n" : string.Empty);
        if (container == "nested") body = string.Join('\n', body.Split('\n').Select(line => $"  {line}"));
        var result = _compiler.Compile("policy Access\n  require authenticated\n" + prefix + body);

        result.Success.ShouldBeTrue();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.AuthorizationRefusalWithoutIdentity).ShouldEqual(gated && withRefusal ? 1 : 0);
    }

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
