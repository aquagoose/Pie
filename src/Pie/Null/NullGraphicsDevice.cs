using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pie.ShaderCompiler;

namespace Pie.Null;

internal sealed class NullGraphicsDevice : GraphicsDevice
{
    public override GraphicsApi Api => GraphicsApi.Null;

    public override Swapchain Swapchain { get; }

    public override GraphicsAdapter Adapter { get;  }

    public override Rectangle Viewport { get; set; }
    public override Rectangle Scissor { get; set; }

    public NullGraphicsDevice(Size winSize)
    {
        Swapchain = new Swapchain()
        {
            Size = winSize
        };
        Adapter = new GraphicsAdapter("Null");
    }

    public override void ClearColorBuffer(Color color)
    {
    }

    public override void ClearColorBuffer(Vector4 color)
    {
    }

    public override void ClearColorBuffer(float r, float g, float b, float a)
    {
    }

    public override void ClearDepthStencilBuffer(ClearFlags flags, float depth, byte stencil)
    {
    }

    public override BlendState CreateBlendState(BlendStateDescription description)
    {
        return new NullBlendState();
    }

    public override unsafe GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T[] data, bool dynamic = false)
    {
        fixed (void* ptr = data)
            return new NullGraphicsBuffer(bufferType, (uint)(Unsafe.SizeOf<T>() * data.Length), ptr, dynamic);
    }

    public override unsafe GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T data, bool dynamic = false)
    {
        return new NullGraphicsBuffer(bufferType, (uint) Unsafe.SizeOf<T>(), Unsafe.AsPointer(ref data), dynamic);
    }

    public override unsafe GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, bool dynamic = false)
    {
        return new NullGraphicsBuffer(bufferType, sizeInBytes, null, dynamic);
    }

    public override unsafe GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, IntPtr data, bool dynamic = false)
    {
        return new NullGraphicsBuffer(bufferType, sizeInBytes, (void*) data, dynamic);
    }

    public override unsafe GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, void* data, bool dynamic = false)
    {
        return new NullGraphicsBuffer(bufferType, sizeInBytes, data, dynamic);
    }

    public override DepthStencilState CreateDepthStencilState(DepthStencilStateDescription description)
    {
        return new NullDepthStencilState();
    }

    public override Framebuffer CreateFramebuffer(params FramebufferAttachment[] attachments)
    {
        return new NullFramebuffer();
    }

    public override InputLayout CreateInputLayout(Shader shader, params InputLayoutDescription[] inputLayoutDescriptions)
    {
        return new NullInputLayout();
    }

    public override RasterizerState CreateRasterizerState(RasterizerStateDescription description)
    {
        return new NullRasterizerState();
    }

    public override SamplerState CreateSamplerState(SamplerStateDescription description)
    {
        return new NullSamplerState();
    }

    public override Shader CreateShader(ShaderAttachment[] attachments, SpecializationConstant[] constants)
    {
        return new NullShader();
    }

    public override unsafe Texture CreateTexture(TextureDescription description)
    {
        return new NullTexture(description, null);
    }

    public override unsafe Texture CreateTexture<T>(TextureDescription description, T[] data)
    {
        fixed (void* ptr = data)
            return new NullTexture(description, ptr);
    }

    public override unsafe Texture CreateTexture<T>(TextureDescription description, T[][] data)
    {
        fixed (void* ptr = PieUtils.Combine(data))
            return new NullTexture(description, ptr);
    }

    public override unsafe Texture CreateTexture(TextureDescription description, IntPtr data)
    {
        return new NullTexture(description, (void*) data);
    }

    public override unsafe Texture CreateTexture(TextureDescription description, void* data)
    {
        return new NullTexture(description, data);
    }

    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
    }

    public override void Dispose()
    {
    }

    public override void Draw(uint vertexCount)
    {
    }

    public override void Draw(uint vertexCount, int startVertex)
    {
    }

    public override void DrawIndexed(uint indexCount)
    {
    }

    public override void DrawIndexed(uint indexCount, int startIndex)
    {
    }

    public override void DrawIndexed(uint indexCount, int startIndex, int baseVertex)
    {
    }

    public override void DrawIndexedInstanced(uint indexCount, uint instanceCount)
    {
    }

    public override void Flush()
    {
    }

    public override void GenerateMipmaps(Texture texture)
    {
    }

    public override unsafe MappedSubresource MapResource(MappableResource resource, MapMode mode)
    {
        return new MappedSubresource((IntPtr) ((NullGraphicsBuffer) resource).Data);
    }

    public override void Present(int swapInterval)
    {
    }

    public override void ResizeSwapchain(Size newSize)
    {
        Swapchain.Size = newSize;
    }

    public override void SetBlendState(BlendState state)
    {
    }

    public override void SetDepthStencilState(DepthStencilState state, uint stencilRef)
    {
    }

    public override void SetFramebuffer(Framebuffer framebuffer)
    {
    }

    public override void SetIndexBuffer(GraphicsBuffer buffer, IndexType type)
    {
    }

    public override void SetPrimitiveType(PrimitiveType type)
    {
    }

    public override void SetRasterizerState(RasterizerState state)
    {
    }

    public override void SetShader(Shader shader)
    {
    }

    public override void SetInputLayout(InputLayout layout)
    {
    }

    public override void SetTexture(uint bindingSlot, Texture texture, SamplerState samplerState)
    {
    }

    public override void SetUniformBuffer(uint bindingSlot, GraphicsBuffer buffer)
    {
    }

    public override void SetVertexBuffer(uint slot, GraphicsBuffer buffer, uint stride)
    {
    }

    public override void UnmapResource(MappableResource resource)
    {
    }

    public override void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T[] data)
    {
    }

    public override void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T data)
    {
    }

    public override void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, IntPtr data)
    {
    }

    public override unsafe void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, void* data)
    {
    }

    public override void UpdateTexture<T>(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width, int height, int depth, T[] data)
    {
    }

    public override void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width, int height, int depth, IntPtr data)
    {
    }

    public override unsafe void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width, int height, int depth, void* data)
    {
    }
}
