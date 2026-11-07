// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.given;

public class a_component_slot : a_compiler
{
    protected CompilationResult<ApplicationSyntax> _result;

    protected void CompileSlot(bool named, string screenData = "", string slotData = "", string field = "status")
    {
        const string Source = """
            behavior Pick
              on click
                when item.FIELD == "open"
                  notify info "Open"
            module Work
              feature Items
                slice StateView Details
                  readmodel Item
                    status String
                  readmodel Other
                    title String
                  query ItemDetails => Item
                  query OtherDetails => Other
                  screen Details
                    SCREEN_DATA
                    component App.Card outer
                      slot content
                        component App.Card inner
                          slot actions
                            SLOT_DATA
                            BINDING
            """;
        var binding = named ? "uses Pick" : "on click\n                  when item.FIELD == \"open\"\n                    notify info \"Open\"";
        _result = _compiler.Compile(Source
            .Replace("SCREEN_DATA", screenData, StringComparison.Ordinal)
            .Replace("SLOT_DATA", slotData, StringComparison.Ordinal)
            .Replace("BINDING", binding, StringComparison.Ordinal)
            .Replace("FIELD", field, StringComparison.Ordinal));
    }
}
