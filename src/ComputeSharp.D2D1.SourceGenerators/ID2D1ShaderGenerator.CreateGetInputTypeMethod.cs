﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ComputeSharp.SourceGeneration.Extensions;
using Microsoft.CodeAnalysis;
using static ComputeSharp.D2D1.SourceGenerators.Diagnostics.DiagnosticDescriptors;

namespace ComputeSharp.D2D1.SourceGenerators;

/// <inheritdoc/>
partial class ID2D1ShaderGenerator
{
    /// <summary>
    /// A helper with all logic to generate the <c>GetInputType</c> method.
    /// </summary>
    private static partial class GetInputType
    {
        /// <summary>
        /// Extracts the input info for the current shader.
        /// </summary>
        /// <param name="structDeclarationSymbol">The input <see cref="INamedTypeSymbol"/> instance to process.</param>
        /// <param name="inputCount">The number of shader inputs to declare.</param>
        /// <param name="inputSimpleIndices">The indicess of the simple shader inputs.</param>
        /// <param name="inputComplexIndices">The indices of the complex shader inputs.</param>
        /// <param name="combinedInputTypes">The combined and serialized input types for each available input.</param>
        /// <param name="diagnostics">The collection of produced <see cref="Diagnostic"/> instances.</param>
        public static void GetInfo(
            INamedTypeSymbol structDeclarationSymbol,
            out int inputCount,
            out ImmutableArray<int> inputSimpleIndices,
            out ImmutableArray<int> inputComplexIndices,
            out ImmutableArray<uint> combinedInputTypes,
            out ImmutableArray<Diagnostic> diagnostics)
        {
            ImmutableArray<Diagnostic>.Builder builder = ImmutableArray.CreateBuilder<Diagnostic>();

            // We need a separate local here so we can keep the original value to apply
            // diagnostics to, but without returning invalid values to the caller which
            // might cause generator errors (eg. -1 would cause other code to just throw).
            int rawInputCount = 0;

            inputCount = 0;

            ImmutableArray<int>.Builder inputSimpleIndicesBuilder = ImmutableArray.CreateBuilder<int>();
            ImmutableArray<int>.Builder inputComplexIndicesBuilder = ImmutableArray.CreateBuilder<int>();

            foreach (AttributeData attributeData in structDeclarationSymbol.GetAttributes())
            {
                switch (attributeData.AttributeClass?.GetFullMetadataName())
                {
                    case "ComputeSharp.D2D1.D2DInputCountAttribute":
                        rawInputCount = (int)attributeData.ConstructorArguments[0].Value!;
                        inputCount = Math.Max(rawInputCount, 0);
                        break;
                    case "ComputeSharp.D2D1.D2DInputSimpleAttribute":
                        inputSimpleIndicesBuilder.Add((int)attributeData.ConstructorArguments[0].Value!);
                        break;
                    case "ComputeSharp.D2D1.D2DInputComplexAttribute":
                        inputComplexIndicesBuilder.Add((int)attributeData.ConstructorArguments[0].Value!);
                        break;
                    default:
                        break;
                }
            }

            inputSimpleIndices = inputSimpleIndicesBuilder.ToImmutable();
            inputComplexIndices = inputComplexIndicesBuilder.ToImmutable();
            combinedInputTypes = ImmutableArray<uint>.Empty;

            // Validate the input count
            if (rawInputCount is not (>= 0 and <= 8))
            {
                builder.Add(InvalidD2DInputCount, structDeclarationSymbol, structDeclarationSymbol);

                goto End;
            }

            // All simple indices must be in range
            if (inputSimpleIndicesBuilder.Concat(inputComplexIndicesBuilder).Any(i => (uint)i >= rawInputCount))
            {
                builder.Add(OutOfRangeInputIndex, structDeclarationSymbol, structDeclarationSymbol);

                goto End;
            }

            HashSet<int> inputSimpleIndicesAsSet = new(inputSimpleIndicesBuilder);
            HashSet<int> inputComplexIndicesAsSet = new(inputComplexIndicesBuilder);

            // All simple indices must be unique
            if (inputSimpleIndicesAsSet.Count != inputSimpleIndicesBuilder.Count)
            {
                builder.Add(RepeatedD2DInputSimpleIndices, structDeclarationSymbol, structDeclarationSymbol);

                goto End;
            }

            // All complex indices must be unique
            if (inputComplexIndicesAsSet.Count != inputComplexIndicesBuilder.Count)
            {
                builder.Add(RepeatedD2DInputComplexIndices, structDeclarationSymbol, structDeclarationSymbol);

                goto End;
            }

            // Simple and complex indices can't have indices in common
            if (inputSimpleIndicesAsSet.Intersect(inputComplexIndicesAsSet).Any())
            {
                builder.Add(InvalidSimpleAndComplexIndicesCombination, structDeclarationSymbol, structDeclarationSymbol);

                goto End;
            }

            // Build the combined input types list now that inputs have been validated
            ImmutableArray<uint>.Builder combinedBuilder = ImmutableArray.CreateBuilder<uint>(inputCount);

            for (int i = 0; i < inputCount; i++)
            {
                // The D2D_INPUT<N>_SIMPLE define is optional, and if omitted, the corresponding input
                // will automatically be of complex type. So we just need to check whether the i-th
                // input is present in the set of explicitly simple inputs. These values will map to
                // values from D2D1PixelShaderInputType, which has Simple and Complex as members.
                // So, simple inputs map to 0, and complex inputs map to 1.
                combinedBuilder.Add(inputSimpleIndicesAsSet.Contains(i) ? 0u : 1u);
            }

            combinedInputTypes = combinedBuilder.MoveToImmutable();

            End:
            diagnostics = builder.ToImmutable();
        }
    }
}
