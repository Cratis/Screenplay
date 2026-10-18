// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

internal static class StoredTypeNames
{
    internal static HashSet<string> For(ApplicationSyntax application)
    {
        var walker = new StoredTypes(application);
        walker.VisitApplication(application);
        return walker.Names;
    }

    sealed class StoredTypes(ApplicationSyntax application) : ScreenplaySyntaxWalker
    {
        internal HashSet<string> Names { get; } = new(StringComparer.Ordinal);

        public override void VisitEvent(EventSyntax syntax)
        {
            foreach (var property in syntax.Properties) Add(property.Type.Name);
            base.VisitEvent(syntax);
        }

        void Add(string name)
        {
            if (!Names.Add(name)) return;
            foreach (var type in application.Types ?? [])
            {
                if (type.Name != name) continue;
                foreach (var property in type.Properties) Add(property.Type.Name);
            }
        }
    }
}
