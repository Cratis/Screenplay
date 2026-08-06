// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents one <c>.play</c> file of an expanded folder structure - where it goes and what is in it.
/// </summary>
/// <param name="RelativePath">The path of the file relative to the root of the structure.</param>
/// <param name="Content">The <c>.play</c> source text of the file.</param>
/// <remarks>
/// Expansion hands back the files rather than writing them, so the same structure can go to disk, into an
/// archive, or straight down a wire without the expansion knowing which.
/// </remarks>
public record PlayFileContent(string RelativePath, string Content);
