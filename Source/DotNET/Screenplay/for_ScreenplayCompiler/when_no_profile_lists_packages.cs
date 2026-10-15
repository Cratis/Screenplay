// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_no_profile_lists_packages : given.a_compiler
{
    [Fact] void should_not_check_blueprints() => _compiler.Compile("module Sales\n  template blueprint Ghost Workspace\n").Diagnostics.ShouldBeEmpty();
}
