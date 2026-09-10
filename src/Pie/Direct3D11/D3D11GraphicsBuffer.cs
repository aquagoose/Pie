using System;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Pie.Direct3D11;

internal sealed class D3D11GraphicsBuffer : GraphicsBuffer
{
    private ID3D11DeviceContext _context;
    private BufferType _type;
    private bool _dynamic;
    
    public override bool IsDisposed { get; protected set; }

    public ID3D11Buffer Buffer;

    public unsafe D3D11GraphicsBuffer(ID3D11Device device, ID3D11DeviceContext context, BufferType type, uint sizeInBytes, void* data, bool dynamic)
    {
        _context = context;
        
        _dynamic = dynamic;
        _type = type;
        
        BindFlags flags;

        switch (type)
        {
            case BufferType.VertexBuffer:
                flags = BindFlags.VertexBuffer;
                PieMetrics.VertexBufferCount++;
                break;
            case BufferType.IndexBuffer:
                flags = BindFlags.IndexBuffer;
                PieMetrics.IndexBufferCount++;
                break;
            case BufferType.UniformBuffer:
                flags = BindFlags.ConstantBuffer;
                PieMetrics.UniformBufferCount++;
                break;
            case BufferType.ShaderStorageBuffer:
                flags = BindFlags.ShaderResource;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        BufferDescription description = new BufferDescription()
        {
            BindFlags = flags,
            ByteWidth = sizeInBytes,
            Usage = dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default,
            CPUAccessFlags = dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None
        };

        if (type == BufferType.ShaderStorageBuffer)
        {
            description.MiscFlags = ResourceOptionFlags.BufferStructured;
            description.StructureByteStride = 1;
        }

        SubresourceData subData = new SubresourceData(data);

        Buffer = device.CreateBuffer(description, data == null ? null : subData);
    }

    public unsafe void Update(uint offsetInBytes, uint sizeInBytes, void* data)
    {
        if (_dynamic)
        {
            Vortice.Direct3D11.MappedSubresource subresource =
                _context.Map(Buffer, 0, Vortice.Direct3D11.MapMode.WriteDiscard);
            Unsafe.CopyBlock((byte*) subresource.DataPointer + (int) offsetInBytes, data, sizeInBytes);
            _context.Unmap(Buffer);
        }
        else
        {
            _context.UpdateSubresource(Buffer, 0,
                new Box((int) offsetInBytes, 0, 0, (int) (sizeInBytes + offsetInBytes), 1, 1), (nint) data, 0, 0);
        }
    }

    public override void Dispose()
    {
        Buffer.Dispose();
        switch (_type)
        {
            case BufferType.VertexBuffer:
                PieMetrics.VertexBufferCount--;
                break;
            case BufferType.IndexBuffer:
                PieMetrics.IndexBufferCount--;
                break;
            case BufferType.UniformBuffer:
                PieMetrics.UniformBufferCount--;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal override MappedSubresource Map(MapMode mode)
    {
        Vortice.Direct3D11.MappedSubresource resource = _context.Map(Buffer, 0, mode.ToDx11MapMode());
        return new MappedSubresource(resource.DataPointer);
    }

    internal override void Unmap()
    {
        _context.Unmap(Buffer);
    }
}