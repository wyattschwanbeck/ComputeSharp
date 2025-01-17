// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from um/d3dcommon.h in the Windows SDK for Windows 10.0.20348.0
// Original source is Copyright © Microsoft. All rights reserved.

namespace TerraFX.Interop.DirectX
{
    internal enum D3D_TESSELLATOR_OUTPUT_PRIMITIVE
    {
        D3D_TESSELLATOR_OUTPUT_UNDEFINED = 0,
        D3D_TESSELLATOR_OUTPUT_POINT = 1,
        D3D_TESSELLATOR_OUTPUT_LINE = 2,
        D3D_TESSELLATOR_OUTPUT_TRIANGLE_CW = 3,
        D3D_TESSELLATOR_OUTPUT_TRIANGLE_CCW = 4,
        D3D11_TESSELLATOR_OUTPUT_UNDEFINED = D3D_TESSELLATOR_OUTPUT_UNDEFINED,
        D3D11_TESSELLATOR_OUTPUT_POINT = D3D_TESSELLATOR_OUTPUT_POINT,
        D3D11_TESSELLATOR_OUTPUT_LINE = D3D_TESSELLATOR_OUTPUT_LINE,
        D3D11_TESSELLATOR_OUTPUT_TRIANGLE_CW = D3D_TESSELLATOR_OUTPUT_TRIANGLE_CW,
        D3D11_TESSELLATOR_OUTPUT_TRIANGLE_CCW = D3D_TESSELLATOR_OUTPUT_TRIANGLE_CCW,
    }
}
