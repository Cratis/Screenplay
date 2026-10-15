// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_template_declares_a_blank_category : given.a_compiler
{
    [Fact] void should_count_it_as_missing() => _compiler.Compile("module Sales\n  screen template Page\n    category\n    body\n").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.TemplateCategoryMissing);
}
