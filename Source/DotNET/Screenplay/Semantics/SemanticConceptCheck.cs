// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Checks one concept's generation and validation declaration without a source document or execution plan.
/// Findings are not whole-application admission and generation implementations are never executed.
/// </summary>
public static partial class SemanticConceptCheck
{
    /// <summary>
    /// Checks authored or programmatically constructed concept syntax, including parse-level generation constraints.
    /// </summary>
    /// <param name="concept">The declaration; source locations may all be identical.</param>
    /// <param name="utcAssessmentYear">The UTC year for deterministic support and proofs, or null for the current UTC year.</param>
    /// <param name="assumeGenerated">Whether an implicit UUID concept is used by a generated property. This does not invent generation for other primitives.</param>
    /// <returns>Construct-targeted findings, proofs, support and the concept's minimum ESM version.</returns>
    public static SemanticConceptCheckResult Check(ConceptSyntax concept, int? utcAssessmentYear, bool assumeGenerated = false)
    {
        var year = utcAssessmentYear ?? DateTimeOffset.UtcNow.Year;
        var findings = ImmutableArray.CreateBuilder<SemanticConceptFinding>();
        if (InvalidYear(year) is { } invalid) return invalid;
        if (concept.Generation is { } generation) findings.AddRange(GenerationStrategyCatalog.CheckDeclaration(concept.Type, generation));
        var (normalized, targets) = Normalize(concept);
        var parseFailed = findings.Any(finding => finding.Severity == DiagnosticSeverity.Error);
        var generates = !parseFailed && (concept.Generation is not null || (assumeGenerated && concept.Type == "Uuid"));
        var inspected = SemanticModelBinder.InspectConceptDeclaration(parseFailed ? normalized with { Generation = null } : normalized, year, generates);
        foreach (var diagnostic in inspected.Diagnostics)
        {
            findings.Add(targets.TryGetValue(diagnostic.Location.Line, out var target)
                ? new(diagnostic.Code, diagnostic.Severity, diagnostic.Message, SemanticConceptFindingTarget.Rule, RuleIndex: target.Index, Pattern: target.Pattern)
                : new(diagnostic.Code, diagnostic.Severity, diagnostic.Message, SemanticConceptFindingTarget.Generation));
        }
        var generationFailed = findings.Any(finding => finding.Severity == DiagnosticSeverity.Error && finding.Target != SemanticConceptFindingTarget.Rule);
        if (inspected.Concept is not { } bound) return new(null, findings.ToImmutable(), [], null, SemanticVersion.V1);

        return Complete(bound, year, generates && !generationFailed, findings, false);
    }

    /// <summary>
    /// Checks a semantic concept's generation contract, match patterns and generation-rule proofs.
    /// </summary>
    /// <param name="concept">A semantic concept, such as one read from an executable model.</param>
    /// <param name="utcAssessmentYear">The UTC year for deterministic support and proofs, or null for the current UTC year.</param>
    /// <param name="assumeGenerated">Whether an implicit UUID concept is used by a generated property. This does not invent generation for other primitives.</param>
    /// <returns>Findings indexed by semantic rule order, proofs, support and the concept's minimum ESM version.</returns>
    public static SemanticConceptCheckResult Check(SemanticConcept concept, int? utcAssessmentYear, bool assumeGenerated = false)
    {
        var year = utcAssessmentYear ?? DateTimeOffset.UtcNow.Year;
        var findings = ImmutableArray.CreateBuilder<SemanticConceptFinding>();
        if (InvalidYear(year) is { } invalid) return invalid;
        var generates = concept.Generation is not null || (assumeGenerated && concept.Primitive == SemanticPrimitiveType.Uuid);
        try
        {
            SemanticGenerationContract.Validate(concept, year);
        }
        catch (InvalidSemanticContract error)
        {
            findings.Add(new(DiagnosticCodes.UnsupportedSemanticSyntax, DiagnosticSeverity.Error, error.Message, SemanticConceptFindingTarget.Generation));
            generates = false;
        }
        for (var index = 0; index < concept.Validations.Length; index++)
        {
            if (concept.Validations[index] is not { Kind: SemanticValidationRuleKind.Matches, Operand: SemanticTextValue text }) continue;
            var pattern = PortableMatchPattern.Check(text.Value);
            if (pattern.Status == PortableMatchStatus.Admitted) continue;
            findings.Add(new(pattern.Code!, pattern.Status == PortableMatchStatus.Deprecated ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, pattern.Description, SemanticConceptFindingTarget.Rule, RuleIndex: index, Pattern: pattern));
        }

        return Complete(concept, year, generates, findings, true);
    }

    static SemanticConceptCheckResult? InvalidYear(int year) => year is >= 1 and <= 9899 ? null :
        new(null, [new(DiagnosticCodes.InvalidGenerationArgument, DiagnosticSeverity.Error, "Generation assessment year must allow the complete 100-year UTC horizon.", SemanticConceptFindingTarget.Generation)], [], null, SemanticVersion.V1);

    static SemanticConceptCheckResult Complete(SemanticConcept concept, int year, bool generates, ImmutableArray<SemanticConceptFinding>.Builder findings, bool addProofFindings)
    {
        var proofs = generates ? RuleConsistency.Check(concept, year) : [];
        if (addProofFindings)
        {
            for (var index = 0; index < proofs.Length; index++)
            {
                if (ProofFinding(concept, proofs[index], index) is { } finding) findings.Add(finding);
            }
        }
        GenerationSupport? support = null;
        if (generates)
        {
            var count = SemanticGenerationContract.SupportCount(concept.Generation, year);
            var bits = count is { } known ? GenerationEntropy.SupportBits(known) : (double?)null;
            support = new(count, bits, count is { } exact ? GenerationEntropy.HasAtLeastBits(exact, GenerationStrategyCatalog.IdentifierMinimumBits) : null, bits is { } entropy ? GenerationEntropy.ValuesAtOnePercentCollision(entropy) : null);
        }

        return new(concept, findings.ToImmutable(), proofs, support, SemanticModelBinder.RequiredConceptVersion(concept, generates));
    }

    static SemanticConceptFinding? ProofFinding(SemanticConcept concept, RuleConsistencyResult proof, int index)
    {
        var strategy = concept.Generation?.Kind ?? "uuid";

        return proof.Status switch
        {
            RuleConsistencyStatus.Refuted => new(DiagnosticCodes.RefutedGeneratedConceptRule, DiagnosticSeverity.Error, $"Generation strategy '{strategy}' for concept '{concept.Name}' violates this rule; witness {System.Text.Json.JsonSerializer.Serialize(proof.Witness)}.", SemanticConceptFindingTarget.Rule, RuleIndex: index),
            RuleConsistencyStatus.Unproven => new(DiagnosticCodes.UnprovenGeneratedConceptRule, DiagnosticSeverity.Warning, $"Generation strategy '{strategy}' for concept '{concept.Name}' has an unproven rule: {proof.Reason}.", SemanticConceptFindingTarget.Rule, RuleIndex: index),
            _ => null
        };
    }
}
