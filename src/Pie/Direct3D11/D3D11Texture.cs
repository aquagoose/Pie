using System;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Pie.Direct3D11;

internal sealed unsafe class D3D11Texture : Texture
{
    private ID3D11DeviceContext _context;

    public ID3D11Resource Texture;
    public ID3D11ShaderResourceView View;

    public override bool IsDisposed { get; protected set; }
    
    public override TextureDescription Description { get; set; }

    public D3D11Texture(ID3D11Device device, ID3D11DeviceContext context, in TextureDescription description, void* data)
    {
        _context = context;
        
        Description = description;
        
        int pitch = PieUtils.CalculatePitch(description.Format, description.Width, out int bpp);

        Vortice.DXGI.Format fmt =
            description.Format.ToDxgiFormat((description.Usage & TextureUsage.ShaderResource) ==
                                            TextureUsage.ShaderResource);
        
        BindFlags flags = BindFlags.None;
        if ((description.Usage & TextureUsage.ShaderResource) == TextureUsage.ShaderResource)
            flags |= BindFlags.ShaderResource;

        if (description.Format is Format.D24_UNorm_S8_UInt or Format.D32_Float or Format.D16_UNorm)
            flags |= BindFlags.DepthStencil;
        else if ((description.Usage & TextureUsage.Framebuffer) == TextureUsage.Framebuffer || description.MipLevels == 0)
            flags |= BindFlags.RenderTarget;

        Vortice.DXGI.Format svFmt = description.Format.ToDxgiFormat(false);

        svFmt = svFmt switch
        {
            Vortice.DXGI.Format.D32_Float => Vortice.DXGI.Format.R32_Float,
            Vortice.DXGI.Format.D16_UNorm => Vortice.DXGI.Format.R16_UNorm,
            Vortice.DXGI.Format.D24_UNorm_S8_UInt => Vortice.DXGI.Format.R24_UNorm_X8_Typeless,
            _ => svFmt
        };
        
        ShaderResourceViewDescription svDesc = new ShaderResourceViewDescription()
        {
            Format = svFmt,
        };
        
        int mipLevels = description.MipLevels == 0
            ? PieUtils.CalculateMipLevels(description.Width, description.Height, description.Depth)
            : description.MipLevels;

        switch (description.TextureType)
        {
            case TextureType.Texture1D:
                Texture1DDescription desc1d = new Texture1DDescription()
                {
                    Width = (uint) description.Width,
                    Format = fmt,
                    MipLevels = (uint) mipLevels,
                    ArraySize = (uint) description.ArraySize,
                    Usage = ResourceUsage.Default,
                    BindFlags = flags,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = description.MipLevels == 0 ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None
                };
                
                Texture = device.CreateTexture1D(desc1d);

                if (description.ArraySize == 1)
                {
                    svDesc.ViewDimension = ShaderResourceViewDimension.Texture1D;
                    svDesc.Texture1D = new Texture1DShaderResourceView()
                    {
                        MipLevels = unchecked((uint) -1),
                        MostDetailedMip = 0
                    };
                }
                else
                {
                    svDesc.ViewDimension = ShaderResourceViewDimension.Texture1DArray;
                    svDesc.Texture1DArray = new Texture1DArrayShaderResourceView()
                    {
                        ArraySize = (uint) description.ArraySize,
                        FirstArraySlice = 0,
                        MipLevels = unchecked((uint) -1),
                        MostDetailedMip = 0
                    };
                }

                break;
            case TextureType.Texture2D:
                Texture2DDescription desc2d = new Texture2DDescription()
                {
                    Width = (uint) description.Width,
                    Height = (uint) description.Height,
                    Format = fmt,
                    MipLevels = (uint) mipLevels,
                    ArraySize = (uint) description.ArraySize,
                    SampleDescription = new SampleDescription(1, 0),
                    //Usage = description.Dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default,
                    Usage = ResourceUsage.Default,
                    BindFlags = flags,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = description.MipLevels == 0 ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None
                };
                
                Texture = device.CreateTexture2D(desc2d);

                if (description.ArraySize == 1)
                {
                    svDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                    svDesc.Texture2D = new Texture2DShaderResourceView()
                    {
                        MipLevels = unchecked((uint) -1),
                        MostDetailedMip = 0
                    };
                }
                else
                {
                    svDesc.ViewDimension = ShaderResourceViewDimension.Texture2DArray;
                    svDesc.Texture2DArray = new Texture2DArrayShaderResourceView()
                    { 
                        MipLevels = unchecked((uint) -1),
                        MostDetailedMip = 0,
                        ArraySize = (uint) description.ArraySize,
                        FirstArraySlice = 0
                    };
                }
                
                break;
            case TextureType.Texture3D:
                Texture3DDescription desc3d = new Texture3DDescription()
                {
                    Width = (uint) description.Width,
                    Height = (uint) description.Height,
                    Depth = (uint) description.Depth,
                    Format = fmt,
                    MipLevels = (uint) mipLevels,
                    Usage = ResourceUsage.Default,
                    BindFlags = flags,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = description.MipLevels == 0 ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None
                };
                
                Texture = device.CreateTexture3D(desc3d);
                
                if (description.ArraySize == 1)
                {
                    svDesc.ViewDimension = ShaderResourceViewDimension.Texture3D;
                    svDesc.Texture3D = new Texture3DShaderResourceView()
                    {
                        MipLevels = unchecked((uint) -1),
                        MostDetailedMip = 0
                    };
                }
                else
                    throw new NotSupportedException("3D texture arrays are not supported.");
                break;
            case TextureType.Cubemap:
                Texture2DDescription desc2dcube = new Texture2DDescription()
                {
                    Width = (uint) description.Width,
                    Height = (uint) description.Height,
                    Format = fmt,
                    MipLevels = (uint) mipLevels,
                    ArraySize = (uint) description.ArraySize * 6, // Multiply by 6 for cubemap
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = flags,
                    CPUAccessFlags = CpuAccessFlags.None,
                    MiscFlags = ResourceOptionFlags.TextureCube | (description.MipLevels == 0 ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None)
                };
                
                Texture = device.CreateTexture2D(desc2dcube);

                if (description.ArraySize == 1)
                {
                    svDesc.ViewDimension = ShaderResourceViewDimension.TextureCube;
                    svDesc.TextureCube = new TextureCubeShaderResourceView()
                    {
                        MipLevels = unchecked((uint) -1),
                        MostDetailedMip = 0
                    };
                }
                else
                    throw new NotImplementedException("Cubemap arrays are not currently supported.");

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // Now upload texture data assuming it is not null.
        if (data != null)
        {
            // The current offset in bytes to look at the data.
            uint currentOffset = 0;

            for (int a = 0; a < description.ArraySize * (description.TextureType == TextureType.Cubemap ? 6 : 1); a++)
            {
                int width = description.Width;
                // While width must always have a width >= 1, height and depth may not always. If the height or depth
                // are 0, this will cause the size calculation to fail, so we must set it to 1 here.
                int height = PieUtils.Max(description.Height, 1);
                int depth = PieUtils.Max(description.Depth, 1);

                // The loop must run at least once, even if the mip levels are 0.
                for (int i = 0; i < PieUtils.Max(1, description.MipLevels); i++)
                {
                    uint currSize = (uint) (width * height * depth * (bpp / 8f));

                    int rowPitch = PieUtils.CalculatePitch(description.Format, width, out _);
                    int depthPitch = PieUtils.CalculatePitch(description.Format, depth, out _);

                    context.UpdateSubresource(Texture,
                        DxUtils.CalcSubresource((uint) i, (uint) a, (uint) mipLevels), null,
                        (IntPtr) ((byte*) data + currentOffset), (uint) rowPitch, (uint) depthPitch);

                    currentOffset += currSize;

                    // Divide the width and height by 2 for each mip level.
                    width /= 2;
                    height /= 2;
                    depth /= 2;

                    if (width < 1)
                        width = 1;
                    if (height < 1)
                        height = 1;
                    if (depth < 1)
                        depth = 1;
                }
            }
        }

        if ((description.Usage & TextureUsage.ShaderResource) == TextureUsage.ShaderResource)
        {
            View = device.CreateShaderResourceView(Texture, svDesc);
        }
    }

    public void Update(int x, int y, int z, int width, int height, int depth, int mipLevel, int arrayIndex, void* data)
    {
        TextureDescription description = Description;

        uint subresource = DxUtils.CalcSubresource((uint) mipLevel, (uint) arrayIndex,
            (uint) (description.MipLevels == 0
                ? PieUtils.CalculateMipLevels(description.Width, description.Height, description.Depth)
                : description.MipLevels));

        int rowPitch = PieUtils.CalculatePitch(description.Format, width, out _);
        int depthPitch = PieUtils.CalculatePitch(description.Format, depth, out _);

        Box box = new Box(x, y, z, x + width, y + height, z + depth + 1);
        
        _context.UpdateSubresource(Texture, subresource, box, (IntPtr) data, (uint) rowPitch, (uint) depthPitch);
    }

    public override void Dispose()
    {
        Texture.Dispose();
        View.Dispose();
    }
}