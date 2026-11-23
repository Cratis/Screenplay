// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

public static partial class EsmGoldenVectors
{
    static readonly ImmutableArray<string> _collectSpecificationNames =
        ["NoPaymentIsWrittenOff", "AFailedFeeFailsNothingButAsksForApproval", "AStartedRunFails"];

    /// <summary>
    /// Gets the ESM 36.0 vector with corrected waiting expectations; the original V36 remains frozen.
    /// </summary>
    public static EsmGoldenVector LifecycleV36R2 => Read(36, "lifecycle-esm-v36-r2.json");

    /// <summary>
    /// Gets the ESM 57.0 vector with complete lifecycle event payloads; the originals V51–V57 remain frozen.
    /// </summary>
    public static EsmGoldenVector LifecycleV57R2 => Read(57, "lifecycle-esm-v57-r2.json");

    /// <summary>
    /// Gets exact, narrowly superseded lifecycle execution outcomes for frozen originals.
    /// This does not waive deserialization, normalization, canonical bytes, hashes or semantic revisions.
    /// </summary>
    public static ImmutableArray<EsmLifecycleVectorDisposition> LifecycleDispositions { get; } =
        [.. CollectDispositions(), .. ParcelDispositions()];

    static IEnumerable<EsmLifecycleVectorDisposition> CollectDispositions() =>
        _collectSpecificationNames.Select(name => new EsmLifecycleVectorDisposition(
            "EsmGoldenVectors.V36",
            SemanticVersion.V36,
            SemanticRevision.Parse("rev1:432ee29dd9f149cbe03050c91b5895fbafe8ddabbad44768c1f36cf4b9941a97"),
            "70666a008fd778a38501c729040d00539bf97d5685d3ff4000d7c1811ece4acc",
            "EsmGoldenVectors.LifecycleV36R2",
            "workflow",
            "Collect",
            name,
            ["Expected the run to complete, but the run waits at 'Final'."],
            "Screenplay#1134 (PR #1350) corrected completion semantics: this branch waits at Final. Screenplay#1366."));

    static IEnumerable<EsmLifecycleVectorDisposition> ParcelDispositions()
    {
        (uint Version, string Revision, string Hash)[] originals =
        [
            (51, "rev1:8faac4b22ea782f5b262f14dcb0fd613cd96f428c0b8eaf51d0910c819199dc6", "5f5024c1074d735446fd14d943ecb11dfc914953d1ac36dfeceaa01dd8109b2e"),
            (52, "rev1:7745fef0483f142e778850dad17ec5b77a0e425b3ee4a4b300ebcb3577c7daa1", "c48cf13bf18241b9fcfbb677cc574bcaf19820f8c205ab5fb7cb19212076dbb4"),
            (53, "rev1:065034a7ef92419bcef53ef12650925c857e01eeb85733fbef62f7c9a245d50d", "80a7201da2e99788fb040166f6cfb0d55d8683643c2841a79014b0a47370fa29"),
            (54, "rev1:2652f58d732fca2f0e4cac7a239348cac582b10cd1a63890258fa2605abd2f4a", "9b5852a8e247afefcd0fc3f12800898e659cf72e8b978d6924ad819ff3ccc3d3"),
            (55, "rev1:909e4fdc367b4ae1db36d4fa59165833a57e823b84a7ae4b4f23988e108f1390", "53ec40893796e32dc7921729d7be7264a9f0e3235961d8e01e097aa272cfe7a8"),
            (56, "rev1:bf2307360f7717f5a93e8e199fc720de2f8fb2836df846f6c856251db2e1319c", "591878f56ba6a68d42a1c72c0dae2bb4b8bef11fc3d89fa7bd43115bfc781fbf"),
            (57, "rev1:203417cbd81e9f691285dfdc1d3cef68ffb2b817216839fe84fdf7f171409464", "cb9570c09a0a9b0b44ae41941a668bd5b081132fe4ffe524a2d8c3643ced2651")
        ];
        return originals.SelectMany(original => new[]
        {
            ParcelDisposition(original, "state machine", "ParcelFlow", "PackingBooksAPickup"),
            ParcelDisposition(original, "workflow", "Pickup", "BookingThenCancelling")
        });
    }

    static EsmLifecycleVectorDisposition ParcelDisposition((uint Version, string Revision, string Hash) original, string kind, string owner, string specification) =>
        new(
            $"EsmGoldenVectors.V{original.Version}",
            new SemanticVersion(original.Version, 0),
            SemanticRevision.Parse(original.Revision),
            original.Hash,
            "EsmGoldenVectors.LifecycleV57R2",
            kind,
            owner,
            specification,
            ["The facts are rejected: Fact 'sem1:59e1d926b1d3f383d118496fca10cbba3f3714f2e28548726a55cbb4f37751a4' does not match a known event contract and its exact property shape."],
            "OrderPacked gained required parcelId in PR #1274; frozen lifecycle fixtures omit it. Screenplay#1367.");
}
