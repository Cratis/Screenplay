// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_the_producer_is_external : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => _result = new ScreenplayCompiler().Compile("import Contracts.E\nmodule M\n  feature F\n    slice StateView View\n      event Own\n      projection P\n        from E\n        from Own");
    [Fact] void should_not_report_external_or_self_edges() => _result.Diagnostics.ShouldBeEmpty();
}
