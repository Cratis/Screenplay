// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Compiles the executable subset of a source-valid application, reporting excluded declarations.
/// </summary>
public interface IAdmissibleSemanticModelCompiler
{
    /// <summary>
    /// Compiles normally, then excludes attributable binder failures and their dependents until binding succeeds.
    /// </summary>
    /// <param name="applicationName">The stable display name of the application.</param>
    /// <param name="documents">The source documents and authoritative identity catalog.</param>
    /// <returns>The compilation and its exclusions; source errors and unattributable failures remain errors.</returns>
    AdmissibleCompilationResult CompileAdmissible(string applicationName, SemanticDocumentSet documents);
}
