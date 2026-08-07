// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_scoped_query : given.a_printer
{
    // 'Mine' and 'All' differ in what the caller sees, not in who may call - and that difference is the
    // access model a reader of the document needs.
    const string Source =
        """
        module Timesheets
          feature Registration
            slice StateView Timesheets
              query Mine => TimesheetReadModel[]
                scoped to identity

              query Everyones => TimesheetReadModel[]
                scoped to global

              query ForTenant => TimesheetReadModel[]

              projection Timesheets => TimesheetReadModel
                from TimesheetStarted key engagementId
                  engagementId = engagementId

              event TimesheetStarted
                engagementId Uuid
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_keep_the_identity_scope() => Query("Mine").Scope.ShouldEqual("identity");
    [Fact] void should_keep_the_global_scope() => Query("Everyones").Scope.ShouldEqual(QuerySyntax.GlobalScope);
    [Fact] void should_print_the_scope() => _roundtrip.Printed.ShouldContain("scoped to identity");

    // The tenant a query runs for is the default, so a query scoped to it says nothing - the absence is
    // what states it, and printing must not invent a scope that was never written.
    [Fact] void should_leave_the_default_scope_unstated() => Query("ForTenant").Scope.ShouldBeNull();
    [Fact] void should_not_print_a_scope_that_was_not_declared() =>
        _roundtrip.Printed.ShouldContain("query ForTenant => TimesheetReadModel[]\n");

    QuerySyntax Query(string name) =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single(_ => _.Name == name);
}
