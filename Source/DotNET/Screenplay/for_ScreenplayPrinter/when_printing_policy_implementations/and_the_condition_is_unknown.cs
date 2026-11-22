// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_policy_implementations;

public class and_the_condition_is_unknown : given.a_printer
{
    ApplicationSyntax _application;
    Exception _error;

    void Establish() => _application = new([], [], [new PolicySyntax("Staff", new UnknownCondition(SourceLocation.Start), null, SourceLocation.Start)], [], SourceLocation.Start);

    void Because() => _error = Catch.Exception(() => _printer.Print(_application));

    [Fact] void should_refuse_the_unknown_condition() => _error.ShouldBeOfExactType<UnsupportedSyntaxForPrinting>();
    [Fact] void should_identify_the_unsupported_condition() => _error.Message.ShouldEqual("The Screenplay printer has no surface form for policy condition 'UnknownCondition'. Teach the printer to write it before it can be printed.");

    sealed record UnknownCondition(SourceLocation Location) : PolicyConditionSyntax(Location);
}
