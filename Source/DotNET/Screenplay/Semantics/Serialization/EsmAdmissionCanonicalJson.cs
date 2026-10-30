// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Reconstructs both the baseline projection and the complete wire bytes. Baseline member
/// and array order is checked by the unchanged strict reader/writer, not by semantic knowledge
/// of extension fields. Opaque ordering remains frozen even after future semantic promotion.
/// </summary>
internal sealed class EsmAdmissionCanonicalJson(JsonElement root, uint major, uint registeredMinor)
{
    internal byte[] Write(bool projection, SemanticRevision? revision) => new EsmCanonicalDocumentWriter(major, registeredMinor, projection, revision).Write(root);
}
