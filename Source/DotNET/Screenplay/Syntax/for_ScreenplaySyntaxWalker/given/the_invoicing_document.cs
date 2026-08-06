// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_ScreenplayCompiler.given;

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.given;

public class the_invoicing_document : Specification
{
    protected ApplicationSyntax _document;

    void Establish() => _document = new ScreenplayCompiler().Compile(Samples.Invoicing).Value!;
}
