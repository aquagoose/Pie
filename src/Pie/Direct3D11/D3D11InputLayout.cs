using System;
using System.Diagnostics;
using System.Text;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Pie.Direct3D11;

internal sealed class D3D11InputLayout : InputLayout
{
    public readonly ID3D11InputLayout Layout;

    public D3D11InputLayout(ID3D11Device device, InputLayoutDescription[] descriptions, D3D11Shader shader)
    {
        Debug.Assert(shader.VertexBytecode != null,
            "The given shader object does not have a valid vertex shader. An input layout requires a valid vertex shader.");
        
        InputElementDescription[] iedesc = new InputElementDescription[descriptions.Length];
        for (uint i = 0; i < iedesc.Length; i++)
        {
            ref InputElementDescription d = ref iedesc[i];
            ref InputLayoutDescription desc = ref descriptions[i];

            Vortice.DXGI.Format fmt = desc.Format.ToDxgiFormat(false);
            
            d = new InputElementDescription()
            {
                SemanticName = "TEXCOORD",
                SemanticIndex = i,
                AlignedByteOffset = desc.Offset,
                Format = fmt,
                Slot = desc.Slot,
                Classification = (InputClassification) desc.InputType,
                InstanceDataStepRate = (uint) desc.InputType
            };
        }

        Descriptions = descriptions;
        Layout = device.CreateInputLayout(iedesc, shader.VertexBytecode);
    }

    public override bool IsDisposed { get; protected set; }

    public override InputLayoutDescription[] Descriptions { get; }

    public override void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        Layout.Dispose();
    }
}