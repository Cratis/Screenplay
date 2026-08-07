// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_combined_policy_condition;

public class and_the_operators_are_mixed_without_a_group : for_ScreenplayPrinter.given.a_printer
{
    const string Source =
        """
        policy CanApprove
          require role "Approver" or role "Manager" and authenticated
        """;

    // 'and' binds tighter than 'or', as it does in a general purpose language, so the manager has to be
    // authenticated and the approver does not. Reading strictly left to right would instead have required
    // everyone to be authenticated.
    const string Shape = "(role:Approver Or (role:Manager And authenticated))";

    for_ScreenplayPrinter.given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_and_tighter_than_or() => Describe(Condition(_roundtrip.Original!)).ShouldEqual(Shape);
    [Fact] void should_print_the_grouping_it_chose() => _roundtrip.Printed.ShouldContain(@"require role ""Approver"" or (role ""Manager"" and authenticated)");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_to_the_same_tree() => Describe(Condition(_roundtrip.Reparsed)).ShouldEqual(Shape);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static PolicyConditionSyntax Condition(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Policies.Single().Condition!;

    static string Describe(PolicyConditionSyntax condition) => condition switch
    {
        AuthenticatedConditionSyntax => "authenticated",
        RoleConditionSyntax role => $"role:{role.Role}",
        ClaimConditionSyntax claim => $"claim:{claim.Claim}",
        LogicalPolicyConditionSyntax logical => $"({Describe(logical.Left)} {logical.Operator} {Describe(logical.Right)})",
        _ => "?"
    };
}
