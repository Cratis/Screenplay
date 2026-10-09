// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

internal static class PurposeCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application)
    {
        var coverage = PurposeCoverage.Slices(application).ToArray();
        var referenced = coverage.SelectMany(item => item.Purposes).Select(reference => reference.Name).ToHashSet(StringComparer.Ordinal);
        var walker = new References();
        walker.VisitApplication(application);
        referenced.UnionWith(walker.Names);
        foreach (var purpose in application.Purposes)
        {
            if (purpose.Basis is null) yield return Diagnostic.Warning(DiagnosticCodes.PurposeWithoutBasis, $"Purpose '{purpose.Name}' has no declared basis (Art. 6(1)); this finding is a prompt to look, not a legal verdict", purpose.Location);
            if (!referenced.Contains(purpose.Name)) yield return Diagnostic.Warning(DiagnosticCodes.UnusedPurpose, $"Purpose '{purpose.Name}' is declared but never referenced", purpose.Location);
        }

        var conceptsIn = PurposeCoverage.ConceptResolver(application);
        foreach (var (slice, purposes) in coverage)
        {
            var concepts = conceptsIn(slice).Where(concept => concept.Attributes.Any(attribute => attribute.Name == "pii")).ToArray();
            if (concepts.Length == 0) continue;
            var covered = application.Purposes.Where(purpose => purposes.Any(reference => reference.Name == purpose.Name)).ToArray();
            if (covered.Length == 0) yield return Diagnostic.Warning(DiagnosticCodes.PersonalDataWithoutPurpose, $"Slice '{slice.Name}' carries pii concepts but has no declared purpose in scope", slice.Location);
            foreach (var purpose in covered)
            {
                if (purpose.Condition is null && concepts.Any(concept => concept.Attributes.Any(attribute => attribute.SpecialCategory is not null)))
                    yield return Diagnostic.Warning(DiagnosticCodes.SpecialDataWithoutCondition, $"Purpose '{purpose.Name}' covers special-category pii in slice '{slice.Name}' but has no condition (Art. 9(2))", purpose.Location);
                if (purpose.Authorization is null && concepts.Any(concept => concept.Attributes.Any(attribute => attribute.Criminal)))
                    yield return Diagnostic.Warning(DiagnosticCodes.CriminalDataWithoutAuthorization, $"Purpose '{purpose.Name}' covers criminal-offence pii in slice '{slice.Name}' but has no authorization (Art. 10)", purpose.Location);
            }
        }
    }

    sealed class References : ScreenplaySyntaxWalker
    {
        internal HashSet<string> Names { get; } = new(StringComparer.Ordinal);

        public override void VisitPurposeReference(PurposeReferenceSyntax syntax) => Names.Add(syntax.Name);
    }
}
