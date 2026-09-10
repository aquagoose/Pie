using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pie.ShaderCompiler;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using DxgiFormat = Vortice.DXGI.Format;
using static Vortice.Direct3D11.D3D11;

namespace Pie.Direct3D11;

internal sealed unsafe class D3D11GraphicsDevice : GraphicsDevice
{
    private DxgiFormat _colorFormat;
    private DxgiFormat? _depthFormat;
    
    private ID3D11Device _device;
    private ID3D11DeviceContext _context;
    
    private IDXGISwapChain _swapChain;
    private ID3D11Texture2D _colorTexture;
    private ID3D11Texture2D _depthStencilTexture;
    private ID3D11RenderTargetView _colorTargetView;
    private ID3D11DepthStencilView _depthStencilTargetView;

    private D3D11Framebuffer _currentFramebuffer;

    public D3D11GraphicsDevice(IntPtr hwnd, Size winSize, GraphicsDeviceOptions options)
    {
        bool debug = options.Debug;
        if (debug && !SdkLayersAvailable())
        {
            debug = false;
            PieLog.Log(LogType.Warning, "Debug has been enabled however no SDK layers have been found. Direct3D debug has therefore been disabled.");
        }

        FeatureLevel[] levels = new [] { FeatureLevel.Level_11_0 };

        DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Singlethreaded;
        if (debug)
            flags |= DeviceCreationFlags.Debug;

        _colorFormat = options.ColorBufferFormat.ToDxgiFormat(false);
        _depthFormat = options.DepthStencilBufferFormat?.ToDxgiFormat(false);

        SwapChainDescription swapChainDescription = new SwapChainDescription()
        {
            Flags = SwapChainFlags.AllowTearing | SwapChainFlags.AllowModeSwitch,
            BufferCount = 2,
            BufferDescription = new ModeDescription((uint) winSize.Width, (uint) winSize.Height, format: _colorFormat),
            BufferUsage = Usage.RenderTargetOutput,
            OutputWindow = hwnd,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            Windowed = true
        };

        Result result;
        
        if ((result = D3D11CreateDeviceAndSwapChain(null, DriverType.Hardware, flags, levels, swapChainDescription,
                out _swapChain, out _device, out _, out _context)).Failure)
        {
            throw new PieException("Failed to create D3D11 device: " + result.Description);
        }

        IDXGIDevice dxgiDevice = _device!.QueryInterface<IDXGIDevice>();
        IDXGIAdapter adapter = dxgiDevice.GetAdapter();

        Adapter = new GraphicsAdapter(adapter.Description.Description);
        
        if ((result = _swapChain!.GetBuffer(0, out _colorTexture)).Failure)
            throw new PieException("Failed to get the back color buffer. " + result.Description);

        _colorTargetView = _device!.CreateRenderTargetView(_colorTexture, null);

        CreateDepthStencilView(winSize);
        
        Viewport = new Rectangle(Point.Empty, winSize);
        Swapchain = new Swapchain()
        {
            Size = winSize
        };

        _context.OMSetRenderTargets(_colorTargetView, _depthStencilTargetView);
    }

    public override GraphicsApi Api => GraphicsApi.D3D11;
    public override Swapchain Swapchain { get; }
    public override GraphicsAdapter Adapter { get; }

    private Rectangle _viewport;
    public override Rectangle Viewport
    {
        get => _viewport;
        set
        {
            _viewport = value;
            _context.RSSetViewport(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
        }
    }

    private Rectangle _scissor;

    public override Rectangle Scissor
    {
        get => _scissor;
        set
        {
            _scissor = value;
            _context.RSSetScissorRect(_scissor.X, _scissor.Y, _scissor.Width, _scissor.Height);
        }
    }

    public override void ClearColorBuffer(Color color) =>
        ClearColorBuffer(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    public override void ClearColorBuffer(Vector4 color) => ClearColorBuffer(color.X, color.Y, color.Z, color.W);

    public override void ClearColorBuffer(float r, float g, float b, float a)
    {
        Color4 color = new Color4(r, g, b, a);
        
        if (_currentFramebuffer != null)
        {
            for (int i = 0; i < _currentFramebuffer.Targets.Length; i++)
                _context.ClearRenderTargetView(_currentFramebuffer.Targets[i], color);
        }
        else
            _context.ClearRenderTargetView(_colorTargetView, color);
    }

    public override void ClearDepthStencilBuffer(ClearFlags flags, float depth, byte stencil)
    {
        //_context.RSSetViewport(Viewport.X, Viewport.Y, Viewport.Width, Viewport.Height);
        DepthStencilClearFlags cf = 0;
        if ((flags & ClearFlags.Depth) == ClearFlags.Depth)
            cf |= DepthStencilClearFlags.Depth;

        if ((flags & ClearFlags.Stencil) == ClearFlags.Stencil)
            cf |= DepthStencilClearFlags.Stencil;

        _context.ClearDepthStencilView(_currentFramebuffer?.DepthStencil ?? _depthStencilTargetView, cf, depth, stencil);
    }

    public override GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T[] data, bool dynamic = false)
    {
        fixed (void* dat = data) 
            return new D3D11GraphicsBuffer(_device, _context, bufferType, (uint) (data.Length * sizeof(T)), dat, dynamic);
    }

    public override GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T data, bool dynamic = false)
    {
        return new D3D11GraphicsBuffer(_device, _context, bufferType, (uint) sizeof(T), Unsafe.AsPointer(ref data), dynamic);
    }

    public override GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, bool dynamic = false)
    {
        return new D3D11GraphicsBuffer(_device, _context, bufferType, sizeInBytes, null, dynamic);
    }

    public override GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, IntPtr data, bool dynamic = false)
    {
        return new D3D11GraphicsBuffer(_device, _context, bufferType, sizeInBytes, data.ToPointer(), dynamic);
    }

    public override GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, void* data, bool dynamic = false)
    {
        return new D3D11GraphicsBuffer(_device, _context, bufferType, sizeInBytes, data, dynamic);
    }

    public override Texture CreateTexture(TextureDescription description)
    {
        return new D3D11Texture(_device, _context, description, null);
    }

    public override Texture CreateTexture<T>(TextureDescription description, T[] data)
    {
        fixed (void* ptr = data)
            return new D3D11Texture(_device, _context, description, ptr);
    }

    public override Texture CreateTexture<T>(TextureDescription description, T[][] data)
    {
        fixed (void* ptr = PieUtils.Combine(data))
            return new D3D11Texture(_device, _context, description, ptr);
    }

    public override Texture CreateTexture(TextureDescription description, IntPtr data)
    {
        return new D3D11Texture(_device, _context, description, data.ToPointer());
    }

    public override Texture CreateTexture(TextureDescription description, void* data)
    {
        return new D3D11Texture(_device, _context, description, data);
    }

    public override Shader CreateShader(ShaderAttachment[] attachments, SpecializationConstant[] constants)
    {
        return new D3D11Shader(_device, _context, attachments, constants);
    }

    public override InputLayout CreateInputLayout(Shader shader, params InputLayoutDescription[] descriptions)
    {
        return new D3D11InputLayout(_device, descriptions, (D3D11Shader) shader);
    }

    public override RasterizerState CreateRasterizerState(RasterizerStateDescription description)
    {
        return new D3D11RasterizerState(_device, description);
    }

    public override BlendState CreateBlendState(BlendStateDescription description)
    {
        return new D3D11BlendState(_device, description);
    }

    public override DepthStencilState CreateDepthStencilState(DepthStencilStateDescription description)
    {
        return new D3D11DepthStencilState(_device, description);
    }

    public override SamplerState CreateSamplerState(SamplerStateDescription description)
    {
        return new D3D11SamplerState(_device, description);
    }

    public override Framebuffer CreateFramebuffer(params FramebufferAttachment[] attachments)
    {
        return new D3D11Framebuffer(_device, attachments);
    }

    public override void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T[] data)
    {
        fixed (void* dat = data)
            ((D3D11GraphicsBuffer) buffer).Update(offsetInBytes, (uint) (data.Length * sizeof(T)), dat);
    }

    public override void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T data)
    {
        ((D3D11GraphicsBuffer) buffer).Update(offsetInBytes, (uint) sizeof(T), Unsafe.AsPointer(ref data));
    }

    public override void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, IntPtr data)
    {
        ((D3D11GraphicsBuffer) buffer).Update(offsetInBytes, sizeInBytes, data.ToPointer());
    }

    public override void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, void* data)
    {
        ((D3D11GraphicsBuffer) buffer).Update(offsetInBytes, sizeInBytes, data);
    }

    public override void UpdateTexture<T>(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z,
        int width, int height, int depth, T[] data)
    {
        fixed (void* dat = data)
            ((D3D11Texture) texture).Update(x, y, z, width, height, depth, mipLevel, arrayIndex, dat);
    }

    public override void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width,
        int height, int depth, IntPtr data)
    {
        ((D3D11Texture) texture).Update(x, y, z, width, height, depth, mipLevel, arrayIndex, data.ToPointer());
    }

    public override void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z,
        int width, int height, int depth, void* data)
    {
        ((D3D11Texture) texture).Update(x, y, z, width, height, depth, mipLevel, arrayIndex, data);
    }

    public override MappedSubresource MapResource(MappableResource resource, MapMode mode)
    {
        return resource.Map(mode);
    }

    public override void UnmapResource(MappableResource resource)
    {
        resource.Unmap();
    }

    public override void SetShader(Shader shader)
    {
        D3D11Shader sh = (D3D11Shader) shader;
        sh.Use();
    }

    public override void SetTexture(uint bindingSlot, Texture texture, SamplerState state)
    {
        D3D11Texture tex = (D3D11Texture) texture;
        _context.PSSetShaderResource(bindingSlot, tex.View);
        _context.PSSetSampler(bindingSlot, ((D3D11SamplerState) state).State);
    }

    public override void SetRasterizerState(RasterizerState state)
    {
        _context.RSSetState(((D3D11RasterizerState) state).State);
    }

    public override void SetBlendState(BlendState state)
    {
        _context.OMSetBlendState(((D3D11BlendState) state).State, null, uint.MaxValue);
    }

    public override void SetDepthStencilState(DepthStencilState state, uint stencilRef)
    {
        _context.OMSetDepthStencilState(((D3D11DepthStencilState) state).State, stencilRef);
    }

    public override void SetPrimitiveType(PrimitiveType type)
    {
        PrimitiveTopology topology = type switch
        {
            PrimitiveType.TriangleList => PrimitiveTopology.TriangleList,
            PrimitiveType.TriangleStrip => PrimitiveTopology.TriangleStrip,
            PrimitiveType.LineList => PrimitiveTopology.LineList,
            PrimitiveType.LineStrip => PrimitiveTopology.LineStrip,
            PrimitiveType.PointList => PrimitiveTopology.PointList,
            PrimitiveType.TriangleListAdjacency => PrimitiveTopology.TriangleListAdjacency,
            PrimitiveType.TriangleStripAdjacency => PrimitiveTopology.TriangleStripAdjacency,
            PrimitiveType.LineListAdjacency => PrimitiveTopology.LineListAdjacency,
            PrimitiveType.LineStripAdjacency => PrimitiveTopology.LineStripAdjacency,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        
        _context.IASetPrimitiveTopology(topology);
    }

    public override void SetInputLayout(InputLayout layout)
    {
        _context.IASetInputLayout(((D3D11InputLayout) layout).Layout);
    }

    public override void SetVertexBuffer(uint slot, GraphicsBuffer buffer, uint stride)
    {
        _context.IASetVertexBuffer(slot, ((D3D11GraphicsBuffer) buffer).Buffer, stride, 0);
    }

    public override void SetIndexBuffer(GraphicsBuffer buffer, IndexType type)
    {
        Vortice.DXGI.Format fmt = type switch
        {
            IndexType.UShort => Vortice.DXGI.Format.R16_UInt,
            IndexType.UInt => Vortice.DXGI.Format.R32_UInt,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        
        _context.IASetIndexBuffer(((D3D11GraphicsBuffer) buffer).Buffer, fmt, 0);
    }

    public override void SetUniformBuffer(uint bindingSlot, GraphicsBuffer buffer)
    {
        D3D11GraphicsBuffer buf = (D3D11GraphicsBuffer) buffer;
        _context.VSSetConstantBuffer(bindingSlot, buf.Buffer);
        _context.PSSetConstantBuffer(bindingSlot, buf.Buffer);
        _context.GSSetConstantBuffer(bindingSlot, buf.Buffer);
        _context.CSSetConstantBuffer(bindingSlot, buf.Buffer);
    }

    public override void SetFramebuffer(Framebuffer framebuffer)
    {
        _currentFramebuffer = (D3D11Framebuffer) framebuffer;
        if (framebuffer == null)
            _context.OMSetRenderTargets(_colorTargetView, _depthStencilTargetView);
        else
        {
            _context.OMSetRenderTargets(_currentFramebuffer.Targets, _currentFramebuffer.DepthStencil);
        }

    }

    public override void Draw(uint vertexCount)
    {
        _context.Draw(vertexCount, 0);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += vertexCount / 3;
    }

    public override void Draw(uint vertexCount, int startVertex)
    {
        _context.Draw(vertexCount, (uint) startVertex);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += vertexCount/ 3;
    }

    public override void DrawIndexed(uint indexCount)
    {
        _context.DrawIndexed(indexCount, 0, 0);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount / 3;
    }

    public override void DrawIndexed(uint indexCount, int startIndex)
    {
        _context.DrawIndexed(indexCount, (uint) startIndex, 0);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount/ 3;
    }

    public override void DrawIndexed(uint indexCount, int startIndex, int baseVertex)
    {
        _context.DrawIndexed(indexCount, (uint) startIndex, baseVertex);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount/ 3;
    }

    public override void DrawIndexedInstanced(uint indexCount, uint instanceCount)
    {
        _context.DrawIndexedInstanced(indexCount, instanceCount, 0, 0, 0);
        PieMetrics.TriCount += indexCount / 3 * instanceCount;
        PieMetrics.DrawCalls++;
    }

    public override void Present(int swapInterval)
    {
        PresentFlags flags = PresentFlags.None;

        if (swapInterval == 0)
            flags |= PresentFlags.AllowTearing;
        _swapChain.Present((uint) swapInterval, flags);
        // ?????? This only seems to happen on AMD but after presentation the render targets go off to floaty land
        // I'm sure usually this is resolved by setting render targets at the start of a frame (like what Easel does)
        // but Pie has no way of knowing when the start of a frame is, so just do it at the end of presentation.
        _context.OMSetRenderTargets(_colorTargetView, _depthStencilTargetView);
        
        PieMetrics.DrawCalls = 0;
        PieMetrics.TriCount = 0;
    }

    public override void ResizeSwapchain(Size newSize)
    {
        _context.Flush();
        _context.UnsetRenderTargets();
        _colorTargetView.Dispose();
        _colorTexture.Dispose();

        if (_depthFormat != null)
        {
            _depthStencilTargetView.Dispose();
            _depthStencilTexture.Dispose();
        }

        _swapChain.ResizeBuffers(0, (uint) newSize.Width, (uint) newSize.Height, Vortice.DXGI.Format.Unknown,
            SwapChainFlags.AllowTearing | SwapChainFlags.AllowModeSwitch);
        _colorTexture = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _colorTargetView = _device.CreateRenderTargetView(_colorTexture);
        CreateDepthStencilView(newSize);

        Swapchain.Size = newSize;
    }

    public override void GenerateMipmaps(Texture texture)
    {
        _context.GenerateMips(((D3D11Texture) texture).View);
    }

    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        _context.Dispatch(groupCountX, groupCountY, groupCountZ);
    }

    public override void Flush()
    {
        _context.Flush();
    }

    public override void Dispose()
    {
        _colorTargetView.Dispose();
        _swapChain.Dispose();
        _device.Dispose();
        _context.Dispose();
    }

    private void CreateDepthStencilView(Size size)
    {
        if (_depthFormat == null)
            return;
        
        Texture2DDescription texDesc = new Texture2DDescription()
        {
            Format = _depthFormat.Value,
            Width = (uint) size.Width,
            Height = (uint) size.Height,
            ArraySize = 1,
            MipLevels = 1,
            BindFlags = BindFlags.DepthStencil,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default
        };

        DepthStencilViewDescription viewDesc = new DepthStencilViewDescription()
        {
            Format = texDesc.Format,
            ViewDimension = DepthStencilViewDimension.Texture2D
        };

        _depthStencilTexture = _device.CreateTexture2D(texDesc);
        _depthStencilTargetView = _device.CreateDepthStencilView(_depthStencilTexture, viewDesc);
    }
}