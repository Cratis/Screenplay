// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_free_screen_names_a_query_by_its_declaration_site : given.a_free_screens_model
{
    void Establish() => Compile("module Orders\n  feature Listing\n    screen OrderList\n      data OrderRow[] via query ListOrders\n    slice StateView ListOrders\n      readmodel OrderRow\n        id String\n      query ListOrders => OrderRow[]\n\nmodule Archive\n  feature Listing\n    slice StateView ListOrders\n      readmodel OldRow\n        id String\n      query ListOrders => OldRow[]\n      uses screen OrderList\n");

    [Fact] void should_resolve_the_query_where_the_screen_is_declared() => With(DiagnosticCodes.AmbiguousReference).Count().ShouldEqual(0);
    [Fact] void should_not_report_an_unknown_query() => With(DiagnosticCodes.UnknownQuery).ShouldBeEmpty();
}
