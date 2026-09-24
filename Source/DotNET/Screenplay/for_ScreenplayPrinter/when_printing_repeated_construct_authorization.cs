// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_repeated_construct_authorization : given.a_printer
{
    const string Source =
        """
        policy Staff
          require role "Staff"
        policy Finance
          require role "Finance"
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                authorize Staff
                authorize Finance
              readmodel Report
                id Uuid
              query ReportById => Report?
                by id Uuid
                authorize Staff
                authorize Finance
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_both_command_gates_in_order() => Names(_roundtrip.Original!.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Authorize!).ShouldEqual("Staff,Finance");
    [Fact] void should_parse_both_query_gates_in_order() => Names(_roundtrip.Original!.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single().Authorize!).ShouldEqual("Staff,Finance");
    [Fact] void should_print_and_reparse_the_combined_gates() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_both_requirements() => _roundtrip.Printed.ShouldContain("authorize Staff and Finance");
    [Fact] void should_round_trip_through_syntax_json()
    {
        var application = _roundtrip.Original!.Value!;
        SyntaxJson.StructurallyEqual(application, SyntaxJson.Deserialize(SyntaxJson.Serialize(application))).ShouldBeTrue();
    }

    static string Names(AuthorizeSyntax authorize) => string.Join(',', authorize.References().Select(reference => reference.Name));
}
