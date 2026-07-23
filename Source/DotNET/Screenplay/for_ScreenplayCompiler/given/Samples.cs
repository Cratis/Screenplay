// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

public static class Samples
{
    public static string Invoicing
    {
        get
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Cratis.Screenplay.for_ScreenplayCompiler.invoicing.play")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string InvoicingStrings
    {
        get
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Cratis.Screenplay.for_ScreenplayCompiler.invoicing.en.strings")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
