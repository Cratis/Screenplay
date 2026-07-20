// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayPrinter.given;

public class a_printer : Specification
{
    protected ScreenplayCompiler _compiler;
    protected ScreenplayPrinter _printer;

    void Establish()
    {
        _compiler = new();
        _printer = new();
    }
}
