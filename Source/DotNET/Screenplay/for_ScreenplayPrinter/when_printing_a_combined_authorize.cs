// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_combined_authorize : given.a_printer
{
    // 'authorize A or B C' is what #68 was about: adjacency means and, 'or' means or, and a flat list with
    // an alternative flag cannot say which of the two groupings was meant.
    const string Source =
        """
        policy IsAccountant
          require authenticated

        policy IsFinance
          require authenticated

        policy OwnsInvoice
          require authenticated

        policy IsAdmin
          require authenticated

        module Invoicing
          feature Invoices
            slice StateChange WriteOff
              command WriteOff
                invoiceId Uuid
                authorize IsAccountant or IsFinance and OwnsInvoice

              command Cancel
                invoiceId Uuid
                authorize (IsAccountant or IsFinance) and IsAdmin
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    // 'and' binds tighter, exactly as it does in a policy condition - one condition grammar, one answer.
    [Fact] void should_bind_and_tighter_than_or() => Describe("WriteOff").ShouldEqual("(IsAccountant Or (IsFinance And OwnsInvoice))");
    [Fact] void should_keep_a_group_that_precedence_would_not_give() => Describe("Cancel").ShouldEqual("((IsAccountant Or IsFinance) And IsAdmin)");
    [Fact] void should_print_the_grouping_it_chose() => _roundtrip.Printed.ShouldContain("authorize IsAccountant or (IsFinance and OwnsInvoice)");
    [Fact] void should_print_the_group_that_is_load_bearing() => _roundtrip.Printed.ShouldContain("authorize (IsAccountant or IsFinance) and IsAdmin");

    // The flat view still answers "which policies are involved", which is all a resolver needs.
    [Fact] void should_still_list_every_policy_it_references() =>
        Authorize("WriteOff").References().Select(_ => _.Name).ShouldContainOnly("IsAccountant", "IsFinance", "OwnsInvoice");

    string Describe(string command) => Describe(Authorize(command).Requirement);

    AuthorizeSyntax Authorize(string command) =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single()
            .Commands.Single(_ => _.Name == command).Authorize!;

    static string Describe(PolicyRequirementSyntax requirement) => requirement switch
    {
        PolicyReferenceSyntax reference => reference.Name,
        LogicalPolicyRequirementSyntax logical => $"({Describe(logical.Left)} {logical.Operator} {Describe(logical.Right)})",
        _ => "?"
    };
}
