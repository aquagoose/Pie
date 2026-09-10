using System;
using System.Drawing;
using System.Numerics;
using Pie.DebugLayer;
using Pie.Direct3D11;
using Pie.Null;
using Pie.OpenGL;
using Pie.ShaderCompiler;

namespace Pie;

/// <summary>
/// A Pie Graphics Device provides all the rendering functions for a given physical graphics device.
/// </summary>
public abstract class GraphicsDevice : IDisposable
{
    /// <summary>
    /// Get the <see cref="GraphicsApi"/> this device is using.
    /// </summary>
    public abstract GraphicsApi Api { get; }

    /// <summary>
    /// Get the <see cref="Swapchain"/> of this device.
    /// </summary>
    public abstract Swapchain Swapchain { get; }
    
    /// <summary>
    /// The <see cref="GraphicsAdapter"/> this device is running on.
    /// </summary>
    public abstract GraphicsAdapter Adapter { get; }

    /// <summary>
    /// Get or set the viewport of this device.
    /// </summary>
    public abstract Rectangle Viewport { get; set; }
    
    /// <summary>
    /// Get or set the scissor rectangle of this device.
    /// </summary>
    public abstract Rectangle Scissor { get; set; }

    /// <summary>
    /// Clears the set Framebuffer's color texture with the given color and flags. If no framebuffer is set, this clears
    /// the back buffer.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    public abstract void ClearColorBuffer(Color color);

    /// <summary>
    /// Clears the set Framebuffer's color texture with the given normalized color and flags. If no framebuffer is set,
    /// this clears the back buffer.
    /// </summary>
    /// <param name="color">The color to clear with. This value should be normalized between 0-1.</param>
    public abstract void ClearColorBuffer(Vector4 color);

    /// <summary>
    /// Clears the set Framebuffer's color texture with the given normalized color and flags. If no framebuffer is set,
    /// this clears the back buffer.
    /// </summary>
    /// <param name="r">The red channel, normalized between 0-1.</param>
    /// <param name="g">The green channel, normalized between 0-1.</param>
    /// <param name="b">The blue channel, normalized between 0-1.</param>
    /// <param name="a">The alpha channel, normalized between 0-1.</param>
    public abstract void ClearColorBuffer(float r, float g, float b, float a);

    /// <summary>
    /// Clears the set Framebuffer's depth stencil texture with the given flags. If no framebuffer is set, this clears
    /// the back buffer.
    /// </summary>
    /// <param name="flags">Which part(s) of the depth-stencil buffer to clear.</param>
    /// <param name="depth">The depth value to clear with.</param>
    /// <param name="stencil">The stencil value to clear with.</param>
    public abstract void ClearDepthStencilBuffer(ClearFlags flags, float depth, byte stencil);

    /// <summary>
    /// Create a graphics buffer with the given type and data.
    /// </summary>
    /// <param name="bufferType">The type of buffer that should be created.</param>
    /// <param name="data">The data itself.</param>
    /// <param name="dynamic">Whether or not this buffer is dynamic.</param>
    /// <typeparam name="T">Any unmanaged type.</typeparam>
    /// <returns>The created graphics buffer.</returns>
    public abstract GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T[] data, bool dynamic = false) where T : unmanaged;
    
    /// <summary>
    /// Create a graphics buffer with the given type and data.
    /// </summary>
    /// <param name="bufferType">The type of buffer that should be created.</param>
    /// <param name="data">The data itself.</param>
    /// <param name="dynamic">Whether or not this buffer is dynamic.</param>
    /// <typeparam name="T">Any unmanaged type.</typeparam>
    /// <returns>The created graphics buffer.</returns>
    public abstract GraphicsBuffer CreateBuffer<T>(BufferType bufferType, T data, bool dynamic = false) where T : unmanaged;

    /// <summary>
    /// Create an empty graphics buffer with the given size.
    /// </summary>
    /// <param name="bufferType">The type of buffer that should be created.</param>
    /// <param name="sizeInBytes">The size, in bytes, that this buffer should be.</param>
    /// <param name="dynamic">Whether or not this buffer is dynamic.</param>
    /// <returns>The created graphics buffer.</returns>
    public abstract GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, bool dynamic = false);

    /// <summary>
    /// Create a graphics buffer with the given type and data.
    /// </summary>
    /// <param name="bufferType">The type of buffer that should be created.</param>
    /// <param name="sizeInBytes">The size, in bytes, that this buffer should be.</param>
    /// <param name="data">The data pointer.</param>
    /// <param name="dynamic">Whether or not this buffer is dynamic.</param>
    /// <returns>The created graphics buffer.</returns>
    public abstract GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, IntPtr data, bool dynamic = false);

    /// <summary>
    /// Create a graphics buffer with the given type and data.
    /// </summary>
    /// <param name="bufferType">The type of buffer that should be created.</param>
    /// <param name="sizeInBytes">The size, in bytes, that this buffer should be.</param>
    /// <param name="data">The data pointer.</param>
    /// <param name="dynamic">Whether or not this buffer is dynamic.</param>
    /// <returns>The created graphics buffer.</returns>
    public abstract unsafe GraphicsBuffer CreateBuffer(BufferType bufferType, uint sizeInBytes, void* data, bool dynamic = false);

    /// <summary>
    /// Create an empty texture with the given description.
    /// </summary>
    /// <param name="description">The description of the texture.</param>
    /// <returns>The created texture.</returns>
    public abstract Texture CreateTexture(TextureDescription description);

    /// <summary>
    /// Create a texture with the given description and data.
    /// </summary>
    /// <param name="description">The description of the texture.</param>
    /// <param name="data">The initial data of the texture.</param>
    /// <typeparam name="T">The data type, typically byte or float. This type should match the <see cref="Format"/> in the <paramref name="description"/>.</typeparam>
    /// <returns>The created texture.</returns>
    public abstract Texture CreateTexture<T>(TextureDescription description, T[] data) where T : unmanaged;
    
    /// <summary>
    /// Create a texture with the given description and <b>array</b> data.
    /// </summary>
    /// <param name="description">The description of the texture.</param>
    /// <param name="data">The initial array data of the texture.</param>
    /// <typeparam name="T">The data type, typically byte or float. This type should match the <see cref="Format"/> in the <paramref name="description"/>.</typeparam>
    /// <returns>The created texture.</returns>
    /// <remarks>As this takes in array data, this method should only be used with array textures and cubemaps.</remarks>
    public abstract Texture CreateTexture<T>(TextureDescription description, T[][] data) where T : unmanaged;

    /// <summary>
    /// Create a texture with the given description and data.
    /// </summary>
    /// <param name="description">The description of the texture.</param>
    /// <param name="data">The pointer to the data.</param>
    /// <returns>The created texture.</returns>
    public abstract Texture CreateTexture(TextureDescription description, IntPtr data);

    /// <summary>
    /// Create a texture with the given description and data.
    /// </summary>
    /// <param name="description">The description of the texture.</param>
    /// <param name="data">The pointer to the data.</param>
    /// <returns>The created texture.</returns>
    public abstract unsafe Texture CreateTexture(TextureDescription description, void* data);

    /// <summary>
    /// Create a shader with the given shader attachments.
    /// </summary>
    /// <param name="attachments">The attachments for this shader.</param>
    /// <param name="constants">Any specialization constants to use in this shader.</param>
    /// <returns>The created shader.</returns>
    public abstract Shader CreateShader(ShaderAttachment[] attachments, SpecializationConstant[] constants = null);

    /// <summary>
    /// Create an input layout which can be used with a vertex buffer.
    /// </summary>
    /// <param name="shader">A shader used to validate the input layout.</param>
    /// <param name="inputLayoutDescriptions">The descriptions for this layout.</param>
    /// <returns>The created input layout.</returns>
    public abstract InputLayout CreateInputLayout(Shader shader, params InputLayoutDescription[] inputLayoutDescriptions);

    /// <summary>
    /// Create a new rasterizer state from the given description.
    /// </summary>
    /// <param name="description">The rasterizer state description.</param>
    /// <returns>The created rasterizer state.</returns>
    public abstract RasterizerState CreateRasterizerState(RasterizerStateDescription description);

    /// <summary>
    /// Create a new blend state from the blend state description.
    /// </summary>
    /// <param name="description">The blend state description to create from.</param>
    /// <returns>The created blend state.</returns>
    public abstract BlendState CreateBlendState(BlendStateDescription description);

    /// <summary>
    /// Create a new depth state from the depth state description.
    /// </summary>
    /// <param name="description">The depth state description to create from.</param>
    /// <returns>The created depth state.</returns>
    public abstract DepthStencilState CreateDepthStencilState(DepthStencilStateDescription description);

    /// <summary>
    /// Create a new sampler state from the sampler state description.
    /// </summary>
    /// <param name="description">The sampler state description to create from.</param>
    /// <returns>The created sampler state.</returns>
    public abstract SamplerState CreateSamplerState(SamplerStateDescription description);

    /// <summary>
    /// Create a framebuffer, also known as a render target, that can be rendered to.
    /// </summary>
    /// <param name="attachments">The framebuffer attachments to attach.</param>
    /// <returns>The created framebuffer.</returns>
    public abstract Framebuffer CreateFramebuffer(params FramebufferAttachment[] attachments);
    
    /// <summary>
    /// Update the given buffer with the given data at the given offset in bytes.
    /// </summary>
    /// <param name="buffer">The buffer to update.</param>
    /// <param name="offsetInBytes">The offset in bytes, if any, where the data will be updated.</param>
    /// <param name="data">The data itself.</param>
    /// <typeparam name="T">Any unmanaged type.</typeparam>
    public abstract void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T[] data) where T : unmanaged;

    /// <summary>
    /// Update the given buffer with the given data at the given offset in bytes.
    /// </summary>
    /// <param name="buffer">The buffer to update.</param>
    /// <param name="offsetInBytes">The offset in bytes, if any, where the data will be updated.</param>
    /// <param name="data">The data itself.</param>
    /// <typeparam name="T">Any unmanaged type.</typeparam>
    public abstract void UpdateBuffer<T>(GraphicsBuffer buffer, uint offsetInBytes, T data) where T : unmanaged;

    /// <summary>
    /// Update the given buffer with the given data at the given offset in bytes.
    /// </summary>
    /// <param name="buffer">The buffer to update.</param>
    /// <param name="offsetInBytes">The offset in bytes, if any, where the data will be updated.</param>
    /// <param name="sizeInBytes">The size in bytes of the data.</param>
    /// <param name="data">The data pointer.</param>
    public abstract void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, IntPtr data);

    /// <summary>
    /// Update the given buffer with the given data at the given offset in bytes.
    /// </summary>
    /// <param name="buffer">The buffer to update.</param>
    /// <param name="offsetInBytes">The offset in bytes, if any, where the data will be updated.</param>
    /// <param name="sizeInBytes">The size in bytes of the data.</param>
    /// <param name="data">The data pointer.</param>
    public abstract unsafe void UpdateBuffer(GraphicsBuffer buffer, uint offsetInBytes, uint sizeInBytes, void* data);

    /// <summary>
    /// Update a region of this texture with the given data.
    /// </summary>
    /// <param name="texture">The texture to update.</param>
    /// <param name="mipLevel">The mip level to update.</param>
    /// <param name="arrayIndex">The array index to update.</param>
    /// <param name="x">The x-offset in pixels of the data.</param>
    /// <param name="y">The y-offset in pixels of the data.</param>
    /// <param name="z">The z-offset in pixels of the data.</param>
    /// <param name="width">The width in pixels of the data.</param>
    /// <param name="height">The height in pixels of the data.</param>
    /// <param name="depth">The depth in pixels of the texture.</param>
    /// <param name="data">The data itself.</param>
    /// <typeparam name="T">The data type, typically byte or float. This type should match the <see cref="Format"/> in the texture's <see cref="TextureDescription"/>.</typeparam>
    public abstract void UpdateTexture<T>(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width,
        int height, int depth, T[] data) where T : unmanaged;

    /// <summary>
    /// Update a region of this texture with the given data.
    /// </summary>
    /// <param name="texture">The texture to update.</param>
    /// <param name="mipLevel">The mip level to update.</param>
    /// <param name="arrayIndex">The array index to update.</param>
    /// <param name="x">The x-offset in pixels of the data.</param>
    /// <param name="y">The y-offset in pixels of the data.</param>
    /// <param name="z">The z-offset in pixels of the data.</param>
    /// <param name="width">The width in pixels of the data.</param>
    /// <param name="height">The height in pixels of the data.</param>
    /// <param name="depth">The depth in pixels of the texture.</param>
    /// <param name="data">The data pointer.</param>
    public abstract void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z, int width,
        int height, int depth, IntPtr data);

    /// <summary>
    /// Update a region of this texture with the given data.
    /// </summary>
    /// <param name="texture">The texture to update.</param>
    /// <param name="mipLevel">The mip level to update.</param>
    /// <param name="arrayIndex">The array index to update.</param>
    /// <param name="x">The x-offset in pixels of the data.</param>
    /// <param name="y">The y-offset in pixels of the data.</param>
    /// <param name="z">The z-offset in pixels of the data.</param>
    /// <param name="width">The width in pixels of the data.</param>
    /// <param name="height">The height in pixels of the data.</param>
    /// <param name="depth">The depth in pixels of the texture.</param>
    /// <param name="data">The data pointer.</param>
    public abstract unsafe void UpdateTexture(Texture texture, int mipLevel, int arrayIndex, int x, int y, int z,
        int width, int height, int depth, void* data);

    /// <summary>
    /// Map the given resource to CPU accessible memory.
    /// </summary>
    /// <param name="resource">The resource to map.</param>
    /// <param name="mode">The CPU access mode of this resource.</param>
    /// <returns>The mapped resource's data.</returns>
    public abstract MappedSubresource MapResource(MappableResource resource, MapMode mode);

    /// <summary>
    /// Unmapped the given mapped resource.
    /// </summary>
    /// <param name="resource">The resource to unmap.</param>
    public abstract void UnmapResource(MappableResource resource);

    /// <summary>
    /// Set the shader that will be used on next draw.
    /// </summary>
    /// <param name="shader">The shader to use.</param>
    public abstract void SetShader(Shader shader);

    /// <summary>
    /// Set the texture that will be used on next draw.
    /// </summary>
    /// <param name="bindingSlot">The binding slot that this texture will be used in.</param>
    /// <param name="texture">The texture to use.</param>
    /// <param name="samplerState">The sampler state to use for this texture.</param>
    public abstract void SetTexture(uint bindingSlot, Texture texture, SamplerState samplerState);

    /// <summary>
    /// Set the rasterizer state that will be used on next draw.
    /// </summary>
    /// <param name="state">The rasterizer state to use.</param>
    public abstract void SetRasterizerState(RasterizerState state);

    /// <summary>
    /// Set the blend state that will be used on next draw.
    /// </summary>
    /// <param name="state">The blend state to use.</param>
    public abstract void SetBlendState(BlendState state);

    /// <summary>
    /// Set the depth-stencil state that will be used on next draw.
    /// </summary>
    /// <param name="state">The depth-stencil state to use.</param>
    /// <param name="stencilRef">The reference value to perform against when performing a stencil test.</param>
    public abstract void SetDepthStencilState(DepthStencilState state, uint stencilRef = 0);

    /// <summary>
    /// Set the primitive type that will be used on next draw.
    /// </summary>
    /// <param name="type">The primitive type to draw with.</param>
    public abstract void SetPrimitiveType(PrimitiveType type);

    /// <summary>
    /// Sets the input layout what will be used on next draw.
    /// </summary>
    /// <param name="layout">The input layout to use.</param>
    public abstract void SetInputLayout(InputLayout layout);
    
    /// <summary>
    /// Set the vertex buffer that will be used on next draw.
    /// </summary>
    /// <param name="slot">The input slot.</param>
    /// <param name="buffer">The buffer to use.</param>
    /// <param name="stride">The stride, in bytes, for the input layout.</param>
    public abstract void SetVertexBuffer(uint slot, GraphicsBuffer buffer, uint stride);

    /// <summary>
    /// Set the index buffer that will be used on next draw.
    /// </summary>
    /// <param name="buffer">The buffer to use.</param>
    /// <param name="type">The type of indices.</param>
    public abstract void SetIndexBuffer(GraphicsBuffer buffer, IndexType type);

    /// <summary>
    /// Set the uniform buffer that will be used on next draw.
    /// </summary>
    /// <param name="bindingSlot"></param>
    /// <param name="buffer">The buffer to use.</param>
    public abstract void SetUniformBuffer(uint bindingSlot, GraphicsBuffer buffer);

    /// <summary>
    /// Set the framebuffer that will be used on next draw. Set as null to use the default back buffer.
    /// </summary>
    /// <param name="framebuffer">The framebuffer to use.</param>
    public abstract void SetFramebuffer(Framebuffer framebuffer);

    /// <summary>
    /// Draw to the screen with the given number of vertices.
    /// </summary>
    /// <param name="vertexCount">The number of vertices.</param>
    public abstract void Draw(uint vertexCount);
    
    /// <summary>
    /// Draw to the screen with the given number of vertices, at the given start vertex.
    /// </summary>
    /// <param name="vertexCount">The number of vertices.</param>
    /// <param name="startVertex">The starting vertex of the vertices to draw.</param>
    public abstract void Draw(uint vertexCount, int startVertex);
    
    /// <summary>
    /// Draw to the screen with the given indices count.
    /// </summary>
    /// <param name="indexCount">The number of indices.</param>
    public abstract void DrawIndexed(uint indexCount);
    
    /// <summary>
    /// Draw to the screen with the given indices count, at the given start index.
    /// </summary>
    /// <param name="indexCount">The number of indices.</param>
    /// <param name="startIndex">The starting index of the indices to draw.</param>
    public abstract void DrawIndexed(uint indexCount, int startIndex);
    
    /// <summary>
    /// Draw to the screen with the given indices count, at the given start index, at the given base vertex.
    /// </summary>
    /// <param name="indexCount">The number of indices.</param>
    /// <param name="startIndex">The starting index of the indices to draw.</param>
    /// <param name="baseVertex">The base vertex of the indices to draw.</param>
    public abstract void DrawIndexed(uint indexCount, int startIndex, int baseVertex);

    /// <summary>
    /// Draw with instancing, with the given indices count and number of instances.
    /// </summary>
    /// <param name="indexCount">The number of indices.</param>
    /// <param name="instanceCount">The number of instances.</param>
    public abstract void DrawIndexedInstanced(uint indexCount, uint instanceCount);
    // TODO: implement all draw instanced functions

    /// <summary>
    /// Present to the screen.
    /// </summary>
    public abstract void Present(int swapInterval);

    /// <summary>
    /// Resize the swapchain.
    /// </summary>
    /// <param name="newSize">The new size of the swapchain.</param>
    public abstract void ResizeSwapchain(Size newSize);

    /// <summary>
    /// Generate mipmaps for the given texture.
    /// </summary>
    /// <param name="texture"></param>
    public abstract void GenerateMipmaps(Texture texture);

    /// <summary>
    /// Dispatch the current compute shader.
    /// </summary>
    /// <param name="groupCountX">The number of thread groups in X.</param>
    /// <param name="groupCountY">The number of thread groups in Y.</param>
    /// <param name="groupCountZ">The number of thread groups in Z.</param>
    public abstract void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ);

    /// <summary>
    /// Force the device to execute all queued commands in the command buffer.
    /// </summary>
    public abstract void Flush();

    /// <summary>
    /// Dispose of this graphics device.
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// Create an OpenGL 3.3 graphics device.
    /// </summary>
    /// <param name="context">The GL context.</param>
    /// <param name="winSize">The size of the window on startup.</param>
    /// <param name="isEs">If enabled, the device will behave like it has been created with an OpenGL ES 3.0 context.</param>
    /// <param name="options">The options for this graphics device, if any.</param>
    /// <returns>The created graphics device.</returns>
    public static GraphicsDevice CreateOpenGL(PieGlContext context, Size winSize, bool isEs, GraphicsDeviceOptions options = default)
    {
        GraphicsDevice device = new GlGraphicsDevice(isEs, context, winSize, options);

        if (options.Debug)
            return new DebugGraphicsDevice(device);
        
        return device;
    }

    /// <summary>
    /// Create a Direct3D 11 graphics device.
    /// </summary>
    /// <param name="hwnd">The HWND pointer.</param>
    /// <param name="winSize">The size of the window on startup.</param>
    /// <param name="options">The options for this graphics device, if any.</param>
    /// <returns>The created graphics device.</returns>
    public static GraphicsDevice CreateD3D11(IntPtr hwnd, Size winSize, GraphicsDeviceOptions options = default)
    {
        GraphicsDevice device = new D3D11GraphicsDevice(hwnd, winSize, options);

        if (options.Debug)
            return new DebugGraphicsDevice(device);

        return device;
    }

    /// <summary>
    /// Create a null graphics device.
    /// </summary>
    /// <param name="winSize">The initial window size. (Use 0x0 if you're not using a window).</param>
    /// <param name="options">The options for this graphics device, if any.</param>
    /// <returns>The created graphics device.</returns>
    public static GraphicsDevice CreateNull(Size winSize, GraphicsDeviceOptions options = default)
    {
        GraphicsDevice device = new NullGraphicsDevice(winSize);
        
        if (options.Debug)
            return new DebugGraphicsDevice(device);
        
        return device;
    }

    /// <summary>
    /// Determine the best graphics API to use for the current platform.
    /// </summary>
    /// <returns></returns>
    public static GraphicsApi GetBestApiForPlatform()
    {
        if (OperatingSystem.IsWindows())
            return GraphicsApi.D3D11;
        else
            return GraphicsApi.OpenGL;
    }
}