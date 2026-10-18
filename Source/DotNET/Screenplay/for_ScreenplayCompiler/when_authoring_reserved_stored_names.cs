// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_reserved_stored_names : given.a_compiler
{
    [Theory]
    [InlineData("eventsource Default", "Event source 'Default' has the reserved stored name 'Default'; choose a stored name other than 'Default'.", 1, 1)]
    [InlineData("eventsource Account\n  id \"Default\"", "Event source 'Account' has the reserved stored name 'Default'; choose a stored name other than 'Default'.", 1, 1)]
    [InlineData("eventsource Account\n  stream All", "Stream 'Account.All' has the reserved stored name 'All'; choose a stored name other than 'All'.", 2, 3)]
    [InlineData("eventsource Account\n  stream Archive\n    id \"All\"", "Stream 'Account.Archive' has the reserved stored name 'All'; choose a stored name other than 'All'.", 2, 3)]
    void should_report_the_reserved_stored_name_at_its_declaration(string source, string message, int line, int column)
    {
        var result = _compiler.Compile(source);
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.ReservedStoredName);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldEqual(message);
        diagnostic.Location.Line.ShouldEqual(line);
        diagnostic.Location.Column.ShouldEqual(column);
    }

    [Theory]
    [InlineData("eventsource Default\n  id \"Account\"")]
    [InlineData("eventsource default")]
    [InlineData("eventsource DEFAULT")]
    [InlineData("eventsource All")]
    [InlineData("eventsource Account\n  stream All\n    id \"Archive\"")]
    [InlineData("eventsource Account\n  stream all")]
    [InlineData("eventsource Account\n  stream ALL")]
    [InlineData("eventsource Account\n  stream Default")]
    [InlineData("eventsource Account\n  stream Archive\n    id \"all\"")]
    void should_accept_other_stored_names_ordinally(string source)
    {
        var result = _compiler.Compile(source);
        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
    }
}
