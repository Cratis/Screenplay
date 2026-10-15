// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    void WriteGeneration(ScreenplayWriter writer, GenerationSyntax generation)
    {
        ImplementationInvariants.Validate(generation);
        writer.Line(GenerationText.Format(generation), generation);
        if (generation.File is null && generation.Code is null) return;
        using (writer.Indent())
        {
            WriteFile(writer, generation.File);
            if (generation.Code is not null) WriteCodeBlock(writer, generation.Code);
        }
    }
}
