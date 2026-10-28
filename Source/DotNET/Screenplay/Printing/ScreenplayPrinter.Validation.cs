// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of imperative validation implementations.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteValidationImplementation(ScreenplayWriter writer, CodeValidateSyntax validation)
    {
        WriteFile(writer, validation.File);
        if (validation.Code is not null) WriteFencedCode(writer, validation.Code);
    }
}
