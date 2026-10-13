// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly HashSet<string> _storedEventNames = new(StringComparer.Ordinal);

        void ValidateStoredEventName(EventSyntax[] declarations)
        {
            var current = declarations[^1];
            var storedName = current.Id ?? current.Name;
            UsesStoredEventNames |= storedName != current.Name;
            foreach (var declaration in declarations)
            {
                if (declaration.Id is { } pin && !StoredEventName.IsValid(pin))
                {
                    Error(DiagnosticCodes.InvalidEventId, $"Event '{declaration.Name}' id pin must use an event-name identifier without surrounding whitespace.", declaration.Location);
                }

                if ((declaration.Id ?? declaration.Name) != storedName)
                {
                    Error(DiagnosticCodes.InvalidEventId, $"Event '{current.Name}' generations must share one effective stored name.", declaration.Location);
                }
            }

            if (!_storedEventNames.Add(storedName))
            {
                Error(DiagnosticCodes.InvalidEventId, $"Event '{current.Name}' stored name '{storedName}' is already used by another event in the application.", current.Location);
            }
        }
    }
}
