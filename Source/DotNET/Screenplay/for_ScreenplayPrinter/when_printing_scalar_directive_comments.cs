// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_scalar_directive_comments : given.a_printer
{
    const string Source = """
        concept State : Enum @pii
          // why this value is personal
          pii reason "Account status" // reason note
          // current state
          active // value note
          inactive

        policy Member
          // access rule
          require authenticated // requirement note

        persona Clerk
          // permission reference
          policy Member // persona note

        theme Aurora
          // supported package
          compatible with core // compatibility note

        ui profile Desktop
          // platform choice
          target platform web // platform note
          // preferred size
          target size expanded // size note
          // dependency list
          packages // packages note
            // component vocabulary
            core // package note
          // base layout
          layout Main // layout note
          // visual theme
          theme Aurora // theme note

        layout Main
          content

        module Shop
          screen template Shell
            navbar contributes Navigation
            main
          feature Ordering
            slice StateView Orders
              screen Home
                action Open
                  // action caption
                  label "Open" // action note
          contribute to Navigation
            navigate to Home
            // navigation caption
            label "Orders" // contribution note
            // navigation order
            order 10 // order note
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_keep_concept_reason_comment() => AssertLine("// why this value is personal", "pii reason \"Account status\" // reason note");
    [Fact] void should_keep_enum_value_comment() => AssertLine("// current state", "active // value note");
    [Fact] void should_keep_policy_requirement_comment() => AssertLine("// access rule", "require authenticated // requirement note");
    [Fact] void should_keep_persona_policy_comment() => AssertLine("// permission reference", "policy Member // persona note");
    [Fact] void should_keep_theme_compatibility_comment() => AssertLine("// supported package", "compatible with core // compatibility note");
    [Fact] void should_keep_profile_target_platform_comment() => AssertLine("// platform choice", "target platform web // platform note");
    [Fact] void should_keep_profile_target_size_comment() => AssertLine("// preferred size", "target size expanded // size note");
    [Fact] void should_keep_profile_packages_comment() => AssertLine("// dependency list", "packages // packages note");
    [Fact] void should_keep_profile_package_comment() => AssertLine("// component vocabulary", "core // package note");
    [Fact] void should_keep_profile_layout_comment() => AssertLine("// base layout", "layout Main // layout note");
    [Fact] void should_keep_profile_theme_comment() => AssertLine("// visual theme", "theme Aurora // theme note");
    [Fact] void should_keep_action_label_comment() => AssertLine("// action caption", "label \"Open\" // action note");
    [Fact] void should_keep_contribution_label_comment() => AssertLine("// navigation caption", "label \"Orders\" // contribution note");
    [Fact] void should_keep_contribution_order_comment() => AssertLine("// navigation order", "order 10 // order note");
    [Fact] void should_print_twice_identically() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    void AssertLine(string comment, string directive)
    {
        _roundtrip.Printed.ShouldContain($"{comment}\n");
        var lines = _roundtrip.Printed.Split('\n');
        lines.Count(line => line.Trim() == comment).ShouldEqual(1);
        lines.Count(line => line.Trim() == directive).ShouldEqual(1);
        var index = Array.FindIndex(lines, line => line.Trim() == directive);
        index.ShouldBeGreaterThan(0);
        lines[index - 1].Trim().ShouldEqual(comment);
        _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    }
}
