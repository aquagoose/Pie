using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pie.ShaderCompiler;
using Silk.NET.OpenGL;

namespace Pie.OpenGL;

internal sealed unsafe class GlGraphicsDevice : GraphicsDevice
{
    private PieGlContext _context;
    internal static GL Gl;
    internal static bool Debug;
    internal static bool IsES;
    
    private RasterizerState _currentRState;
    private BlendState _currentBState;
    private DepthStencilState _currentDStencilState;
    private Silk.NET.OpenGL.PrimitiveType _glType;
    private PrimitiveType _currentPType;
    private DrawElementsType _currentEType;
    private uint _currentShader;

    private GlInputLayout _currentInputLayout;
    
    private (GlGraphicsBuffer buffer, uint stride)[] _currentVertexBuffers;
    private GlGraphicsBuffer _currentIndexBuffer;
    
    private int _eTypeSize;
    private bool _primitiveTypeInitialized;
    private bool _framebufferSet;

    private int _defaultFramebufferId;
    
    public GlGraphicsDevice(bool es, PieGlContext context, Size winSize, GraphicsDeviceOptions options)
    {
        _context = context;
        Gl = GL.GetApi(context.GetProcFunc);
        Debug = options.Debug;
        IsES = es;

        Api = IsES ? GraphicsApi.OpenGLES : GraphicsApi.OpenGL;
        
        Swapchain = new Swapchain()
        {
            Size = winSize
        };

        Gl.GetInteger(GetPName.DrawFramebufferBinding, out _defaultFramebufferId);

        Viewport = new Rectangle(Point.Empty, winSize);

        Adapter = new GraphicsAdapter(Gl.GetStringS(StringName.Renderer));

        // TODO: Get the value from opengl rather than a set value.
        _currentVertexBuffers = new (GlGraphicsBuffer, uint)[16];
        
        if (Debug)
        {
            Gl.Enable(EnableCap.DebugOutput);
            Gl.Enable(EnableCap.DebugOutputSynchronous);
            Gl.DebugMessageCallback(DebugCallback, null);
        }

        //SpirvSupported = Gl.IsExtensionPresent("ARB_gl_spirv");
    }
    
    public override GraphicsApi Api { get; }
    public override Swapchain Swapchain { get; }
    public override GraphicsAdapter Adapter { get; }

    private Rectangle _viewport;
    public override Rectangle Viewport
    {
        get => _viewport;
        set
        {
            Gl.Viewport(value.X, _framebufferSet ? value.Y : Swapchain.Size.Height - (value.Y + value.Height), (uint) value.Width, (uint) value.Height);
            _viewport = value;
        }
    }

    private Rectangle _scissor;

    public override Rectangle Scissor
    {
        get => _scissor;
        set
        {
            Gl.Scissor(value.X, Swapchain.Size.Height - (value.Y + value.Height), (uint) value.Width, (uint) value.Height);
            _scissor = value;
        }
    }

    public override void ClearColorBuffer(Color color) =>
        ClearColorBuffer(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    public override void ClearColorBuffer(Vector4 color) => ClearColorBuffer(color.X, color.Y, color.Z, color.W);

    public override void ClearColorBuffer(float r, float g, float b, float a)
    {
        Gl.ClearColor(r, g, b, a);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public override void ClearDepthStencilBuffer(ClearFlags flags, float depth, byte stencil)
    {
        uint mask = 0;
        if ((flags & ClearFlags.Depth) == ClearFlags.Depth)
            mask |= (uint) ClearBufferMask.DepthBufferBit;
        if ((flags & ClearFlags.Stencil) == ClearFlags.Stencil)
            mask |= (uint) ClearBufferMask.StencilBufferBit;
        
        Gl.ClearDepth(depth);
        Gl.ClearStencil(stencil);
        Gl.Clear(mask);
    }

    private void InvalidateCaches()
    {
        _currentRState = null;
        _currentBState = null;
        PieMetrics.DrawCalls = 0;
        PieMetrics.TriCount = 0;
    }

    public override GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T[] data, bool dynamic = false)
    {
        fixed (void* dat = data)
            return new GlGraphicsBuffer(bufferType, (uint) (data.Length * sizeof(T)), dat, dynamic);
    }

    public override GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T data, bool dynamic = false)
    {
        return new GlGraphicsBuffer(bufferType, (uint) sizeof(T), Unsafe.AsPointer(ref data), dynamic);
    }

    public override GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, bool dynamic = false)
    {
        return new GlGraphicsBuffer(bufferType, sizeInBytes, null, dynamic);
    }

    public override GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, IntPtr data, bool dynamic = false)
    {
        return new GlGraphicsBuffer(bufferType, sizeInBytes, data.ToPointer(), dynamic);
    }

    public override GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, void* data, bool dynamic = false)
    {
        return new GlGraphicsBuffer(bufferType, sizeInBytes, data, dynamic);
    }

    public override Texture CreateTexture(TextureDescription description)
    {
        return GlTexture.CreateTexture(description, null);
    }

    public override Texture CreateTexture<T>(TextureDescription description, T[] data)
    {
        fixed (void* ptr = data)
            return GlTexture.CreateTexture(description, ptr);
    }

    public override Texture CreateTexture<T>(TextureDescription description, T[][] data)
    {
        fixed (void* ptr = PieUtils.Combine(data))
            return GlTexture.CreateTexture(description, ptr);
    }

    public override Texture CreateTexture(TextureDescription description, IntPtr data)
    {
        return GlTexture.CreateTexture(description, data.ToPointer());
    }

    public override Texture CreateTexture(TextureDescription description, void* data)
    {
        return GlTexture.CreateTexture(description, data);
    }

    public override Shader CreateShader(ShaderAttachment[] attachments, SpecializationConstant[] constants)
    {
        return new GlShader(attachments, constants);
    }

    public override InputLayout CreateInputLayout(Shader shader, params InputLayoutDescription[] descriptions)
    {
        return new GlInputLayout(descriptions);
    }

    public override RasterizerState CreateRasterizerState(RasterizerStateDescription description)
    {
        return new GlRasterizerState(description);
    }

    public override BlendState CreateBlendState(BlendStateDescription description)
    {
        return new GlBlendState(description);
    }

    public override DepthStencilState CreateDepthStencilState(DepthStencilStateDescription description)
    {
        return new GlDepthStencilState(description);
    }

    public override SamplerState CreateSamplerState(SamplerStateDescription description)
    {
        return new GlSamplerState(description);
    }

    public override Framebuffer CreateFramebuffer(params FramebufferAttachment[] attachments)
    {
        return new GlFramebuffer(attachments);
    }

    public override void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T[] data)
    {
        fixed (void* dat = data)
            ((GlGraphicsBuffer) buffer).Update(offsetInBytes, (uint) (data.Length * sizeof(T)), dat);
    }

    public override void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T data)
    {
        ((GlGraphicsBuffer) buffer).Update(offsetInBytes, (uint) sizeof(T), Unsafe.AsPointer(ref data));
    }

    public override void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, IntPtr data)
    {
        ((GlGraphicsBuffer) buffer).Update(offsetInBytes, sizeInBytes, data.ToPointer());
    }

    public override void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, void* data)
    {
        ((GlGraphicsBuffer) buffer).Update(offsetInBytes, sizeInBytes, data);
    }

    public override void UpdateTexture<T>(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z,
        int width, int height, int depth, T[] data)
    {
        fixed (void* dat = data)
            ((GlTexture) texture).Update(x, y, z, width, height, depth, mipLevel, arrayIndex, dat);
    }

    public override void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width,
        int height, int depth, IntPtr data)
    {
        ((GlTexture) texture).Update(x, y, z, width, height, depth, mipLevel, arrayIndex, data.ToPointer());
    }

    public override void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z,
        int width, int height, int depth, void* data)
    {
        ((GlTexture) texture).Update(x, y, z, width, height, depth, mipLevel, arrayIndex, data);
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
        GlShader glShader = (GlShader) shader;
        if (glShader.Handle == _currentShader)
            return;
        Gl.UseProgram(glShader.Handle);
        _currentShader = glShader.Handle;
    }

    public override void SetTexture(uint bindingSlot, Texture texture, SamplerState state)
    {
        GlTexture glTex = (GlTexture) texture;
        //if (glTex.Handle == _boundTexture && bindingSlot == _bindingSlot)
        //    return;
        //_boundTexture = (int) glTex.Handle;
        //_bindingSlot = (int) bindingSlot;
        Gl.ActiveTexture(TextureUnit.Texture0 + (int) bindingSlot);
        Gl.BindTexture(glTex.Target, glTex.Handle);
        Gl.BindSampler(bindingSlot, ((GlSamplerState) state).Handle);
    }

    public override void SetRasterizerState(RasterizerState state)
    {
        if (_currentRState != null && _currentRState.Equals(state))
            return;
        _currentRState = state;
        ((GlRasterizerState) state).Set();
    }

    public override void SetBlendState(BlendState state)
    {
        if (_currentBState != null && _currentBState.Equals(state))
            return;
        _currentBState = state;
        ((GlBlendState) state).Set();
    }

    public override void SetDepthStencilState(DepthStencilState state, uint stencilRef)
    {
        if (_currentDStencilState != null && _currentDStencilState.Equals(state))
            return;
        _currentDStencilState = state;
        ((GlDepthStencilState) state).Set((int) stencilRef);
    }

    public override void SetPrimitiveType(PrimitiveType type)
    {
        if (_primitiveTypeInitialized && _currentPType == type)
            return;
        _primitiveTypeInitialized = true;
        _currentPType = type;
        _glType = type switch
        {
            PrimitiveType.TriangleList => Silk.NET.OpenGL.PrimitiveType.Triangles,
            PrimitiveType.TriangleStrip => Silk.NET.OpenGL.PrimitiveType.TriangleStrip,
            PrimitiveType.LineList => Silk.NET.OpenGL.PrimitiveType.Lines,
            PrimitiveType.LineStrip => Silk.NET.OpenGL.PrimitiveType.LineStrip,
            PrimitiveType.PointList => Silk.NET.OpenGL.PrimitiveType.Points,
            PrimitiveType.TriangleListAdjacency => Silk.NET.OpenGL.PrimitiveType.TrianglesAdjacency,
            PrimitiveType.TriangleStripAdjacency => Silk.NET.OpenGL.PrimitiveType.TriangleStripAdjacency,
            PrimitiveType.LineListAdjacency => Silk.NET.OpenGL.PrimitiveType.LinesAdjacency,
            PrimitiveType.LineStripAdjacency => Silk.NET.OpenGL.PrimitiveType.LineStripAdjacency,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public override void SetInputLayout(InputLayout layout)
    {
        _currentInputLayout = (GlInputLayout) layout;
    }

    public override void SetVertexBuffer(uint slot, GraphicsBuffer buffer, uint stride)
    {
        GlGraphicsBuffer glBuf = (GlGraphicsBuffer) buffer;

        _currentVertexBuffers[slot] = (glBuf, stride);
    }

    public override void SetIndexBuffer(GraphicsBuffer buffer, IndexType type)
    {
        GlGraphicsBuffer glBuf = (GlGraphicsBuffer) buffer;

        _currentIndexBuffer = glBuf;

        switch (type)
        {
            case IndexType.UShort:
                _currentEType = DrawElementsType.UnsignedShort;
                _eTypeSize = 2;
                break;
            case IndexType.UInt:
                _currentEType = DrawElementsType.UnsignedInt;
                _eTypeSize = 4;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public override void SetUniformBuffer(uint bindingSlot, GraphicsBuffer buffer)
    {
        Gl.BindBufferBase(BufferTargetARB.UniformBuffer, bindingSlot, ((GlGraphicsBuffer) buffer).Handle);
    }

    public override void SetFramebuffer(Framebuffer framebuffer)
    {
        if (framebuffer == null)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint) _defaultFramebufferId);
            _framebufferSet = false;
            return;
        }

        GlFramebuffer fb = (GlFramebuffer) framebuffer;
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fb.Handle);
        _framebufferSet = true;
    }

    public override void Draw(uint vertexCount)
    {
        BindBuffersWithVao();
        
        Gl.DrawArrays(_glType, 0, vertexCount);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += vertexCount / 3;
    }

    public override void Draw(uint vertexCount, int startVertex)
    {
        BindBuffersWithVao();
        
        Gl.DrawArrays(_glType, startVertex, vertexCount);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += vertexCount / 3;
    }

    public override void DrawIndexed(uint indexCount)
    {
        BindBuffersWithVao();
        
        Gl.DrawElements(_glType, indexCount, _currentEType, null);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount / 3;
    }

    public override void DrawIndexed(uint indexCount, int startIndex)
    {
        BindBuffersWithVao();
        
        Gl.DrawElements(_glType, indexCount, _currentEType, (void*) (startIndex * _eTypeSize));
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount / 3;
    }

    public override void DrawIndexed(uint indexCount, int startIndex, int baseVertex)
    {
        BindBuffersWithVao();
        
        Gl.DrawElementsBaseVertex(_glType, indexCount, _currentEType, (void*) (startIndex * _eTypeSize), baseVertex);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount / 3;
    }

    public override void DrawIndexedInstanced(uint indexCount, uint instanceCount)
    {
        BindBuffersWithVao();
        
        Gl.DrawElementsInstanced(_glType, indexCount, _currentEType, null, instanceCount);
        PieMetrics.DrawCalls++;
        PieMetrics.TriCount += indexCount / 3 * instanceCount;
    }

    public override void Present(int swapInterval)
    {
        _context.Present(swapInterval);
        InvalidateCaches();
    }

    public override void ResizeSwapchain(Size newSize)
    {
        // OpenGL has no swapchain so nothing to resize.
        Swapchain.Size = newSize;
        // Lol, this seems stupid, but it's to ensure the same behaviour between all backends when resizing the swapchain
        // (forcing the viewport to remain in the top left, when OpenGL wants it to remain in the bottom left)
        // This is the easiest option since setting "Viewport" forces it to calculate for a top-left origin point.
        Viewport = _viewport;
    }

    public override void GenerateMipmaps(Texture texture)
    {
        GlTexture tex = (GlTexture) texture;
        Gl.BindTexture(tex.Target, tex.Handle);
        Gl.GenerateMipmap(tex.Target);
    }

    public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        Gl.DispatchCompute(groupCountX, groupCountY, groupCountZ);
    }

    public override void Flush()
    {
        Gl.Flush();
    }

    public override void Dispose()
    {
        
    }

    private void BindBuffersWithVao()
    {
        Gl.BindVertexArray(_currentInputLayout.VertexArray);

        for (uint i = 0; i < _currentVertexBuffers.Length; i++)
        {
            if (_currentVertexBuffers[i].buffer == null)
                continue;
            
            ref (GlGraphicsBuffer buffer, uint stride) buf = ref _currentVertexBuffers[i];
            
            Gl.BindVertexBuffer(i, buf.buffer.Handle, 0, buf.stride);
        }
        
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _currentIndexBuffer.Handle);
    }

    private void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
    {
        string msg = Marshal.PtrToStringAnsi(message);
        DebugType debugType = (DebugType) type;
        LogType logType = debugType switch
        {
            DebugType.DontCare => LogType.Verbose,
            DebugType.DebugTypeError => LogType.Critical,
            DebugType.DebugTypeDeprecatedBehavior => LogType.Error,
            DebugType.DebugTypeUndefinedBehavior => LogType.Critical,
            DebugType.DebugTypePortability => LogType.Warning,
            DebugType.DebugTypePerformance => LogType.Warning,
            DebugType.DebugTypeOther => LogType.Verbose,
            DebugType.DebugTypeMarker => LogType.Debug,
            DebugType.DebugTypePushGroup => LogType.Verbose,
            DebugType.DebugTypePopGroup => LogType.Verbose,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        PieLog.Log(logType, msg);
    }
}