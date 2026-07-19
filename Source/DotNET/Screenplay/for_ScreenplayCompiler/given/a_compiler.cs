// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

public class a_compiler : Specification
{
    protected ScreenplayCompiler _compiler;

    void Establish() => _compiler = new();
}
