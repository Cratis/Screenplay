// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Extensions.for_ExtensionLock;

public class when_parsing_a_malformed_lock
{
    static string[] Codes(string text) => [.. ExtensionLock.Parse(text).Diagnostics.Select(diagnostic => diagnostic.Code)];

    [Fact] void should_require_the_version_line() => Codes("extension \"a\"\n").ShouldContain(DiagnosticCodes.InvalidExtensionLock);
    [Fact] void should_report_an_incomplete_entry() => Codes("lock 1\nextension \"a\"\n  version 1.0.0\n").ShouldEqual([DiagnosticCodes.InvalidExtensionLock]);
    [Fact] void should_report_an_unknown_line() => Codes("lock 1\nwhat\n").ShouldEqual([DiagnosticCodes.InvalidExtensionLock]);
    [Fact] void should_accept_an_empty_lock() => Codes("lock 1\n").ShouldBeEmpty();
}
