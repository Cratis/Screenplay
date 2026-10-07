// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_printing_declared_dependencies : Specification
{
    ApplicationSyntax _original;
    ApplicationSyntax _reparsed;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _original = compiler.Parse("module Payroll\n  description \"Pays people\"\n  depends on Timesheets\n  depends on Engagements\n  feature Handover\n    description \"Transfers hours\"\n    depends on Runs\n    depends on Timesheets.Approval\n  feature Runs\nmodule Timesheets\n  feature Approval\nmodule Engagements\n").Value!;
        _printed = new ScreenplayPrinter().Print(_original);
        _reparsed = compiler.Parse(_printed).Value!;
    }

    [Fact] void should_print_module_targets_after_description() => _printed.ShouldContain("description \"Pays people\"\n  depends on Timesheets\n  depends on Engagements");
    [Fact] void should_print_feature_targets_after_description() => _printed.ShouldContain("description \"Transfers hours\"\n    depends on Runs\n    depends on Timesheets.Approval");
    [Fact] void should_round_trip_all_syntax() => SyntaxJson.StructurallyEqual(_original, _reparsed).ShouldBeTrue();
}
