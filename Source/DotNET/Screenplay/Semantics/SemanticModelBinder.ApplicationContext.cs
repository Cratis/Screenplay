// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // The declared time-zone members of the application context (decision 0132), readable only by 'calendar from'.
        readonly List<string> _contextMembers = [];

        void BindApplicationContext()
        {
            if (syntax.Context is null) return;
            if (!AllowClockOperands)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Application context members are not yet admitted (decision 0132) (#824).", syntax.Context.Location);
                return;
            }

            _contextMembers.AddRange(syntax.Context.Members.Select(member => member.Name));
        }

        bool IsCalendarContextMember(string member) =>
            member == SemanticTypedContextCatalog.CalendarContextMember || _contextMembers.Contains(member, StringComparer.Ordinal);

        string TrustedCalendarMembers() => _contextMembers.Count == 0
            ? $"the only trusted member is '$context.{SemanticTypedContextCatalog.CalendarContextMember}'"
            : $"the trusted members are {string.Join(", ", new[] { SemanticTypedContextCatalog.CalendarContextMember }.Concat(_contextMembers).Select(name => $"'$context.{name}'"))}";
    }
}
