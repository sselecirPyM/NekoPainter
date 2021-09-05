using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using System.Numerics;
using SharpGen.Runtime;

namespace CanvasRendering
{
    public class DeviceResources
    {
        public DeviceResources()
        {
            CreateDeviceResources();
        }

        public void CreateDeviceResources()
        {

            FeatureLevel[] featureLevels = new[]
            {
                //FeatureLevel.Level_12_2,
                //FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2,
                FeatureLevel.Level_9_1,
            };
            var hr = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.None, featureLevels, out ID3D11Device device, out ID3D11DeviceContext deviceContext);
            string s = hr.ToString();
            this.device = device.QueryInterface<ID3D11Device5>();
            this.d3dContext = deviceContext.QueryInterface<ID3D11DeviceContext3>();
            device.Dispose();
            deviceContext.Dispose();
        }

        public void CreateWindowSizeDependentResources()
        {
            ID3D11RenderTargetView[] nullViews = { };
            d3dContext.OMSetRenderTargets(nullViews);
            renderTargetView1?.Dispose();
            depthStencilView?.Dispose();
            d3dContext.Flush();

            UpdateRenderTargetSize();

            m_d3dRenderTargetSize = m_outputSize;
            int width = (int)Math.Round(m_d3dRenderTargetSize.X);
            int height = (int)Math.Round(m_d3dRenderTargetSize.Y);
            if (swapChain != null)
            {
                var hr = swapChain.ResizeBuffers(c_frameCount, width, height, swapChainFormat, swapChainFlags);
                if (hr.Failure)
                {
                    throw new Exception(hr.ToString());
                }
            }
            else
            {
                SwapChainDescription1 swapChainDescription1 = new SwapChainDescription1
                {
                    Width = width,
                    Height = height,
                    Format = swapChainFormat,
                    Stereo = false,
                    SampleDescription = new SampleDescription
                    {
                        Count = 1,
                        Quality = 0,
                    },
                    Usage = Usage.RenderTargetOutput,
                    BufferCount = c_frameCount,
                    SwapEffect = SwapEffect.FlipSequential,
                    Flags = swapChainFlags,
                    Scaling = Scaling.Stretch,
                    AlphaMode = AlphaMode.Ignore,
                };
                dxgiDevice3?.Dispose();
                dxgiDevice3 = device.QueryInterface<IDXGIDevice3>();
                dxgiDevice3.MaximumFrameLatency = 1;
                dxgiAdapter = dxgiDevice3.GetAdapter();
                dxgiFactory4 = dxgiAdapter.GetParent<IDXGIFactory4>();
                var swapChain1 = dxgiFactory4.CreateSwapChainForHwnd(device, hwnd, swapChainDescription1);
                swapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
                swapChain1.Dispose();
            }

            ID3D11Texture2D1 backBaffer = swapChain.GetBuffer<ID3D11Texture2D1>(0);
            renderTargetView1 = device.CreateRenderTargetView1(backBaffer);
            backBaffer.Dispose();
            ID3D11Texture2D1 depthStencil = device.CreateTexture2D1(new Texture2DDescription1(Format.D24_UNorm_S8_UInt, width, height, 1, 0, BindFlags.DepthStencil));
            depthStencilView = device.CreateDepthStencilView(depthStencil);
            depthStencil.Dispose();
        }

        void UpdateRenderTargetSize()
        {
            m_outputSize = m_logicalSize;

            // 防止创建大小为零的 DirectX 内容。
            m_outputSize.X = Math.Max(m_outputSize.X, 1);
            m_outputSize.Y = Math.Max(m_outputSize.Y, 1);
        }

        public void Present()
        {
            var hr = swapChain.Present(0, PresentFlags.AllowTearing, new PresentParameters());
            d3dContext.DiscardView1(renderTargetView1);
            d3dContext.DiscardView1(depthStencilView);
            if (hr.Failure)
                throw new Exception(hr.ToString());
        }

        public void SetSwapChainPanel(IntPtr hwnd, Vector2 logicalSize)
        {
            this.hwnd = hwnd;
            this.m_logicalSize = logicalSize;

            CreateWindowSizeDependentResources();
        }

        public void SetLogicalSize(Vector2 size)
        {
            if (size != m_logicalSize)
            {
                m_logicalSize = size;
                CreateWindowSizeDependentResources();
            }
        }

        public ID3D11SamplerState GetSamplerState(SamplerState samplerState)
        {
            if (samplerStates.TryGetValue(samplerState, out ID3D11SamplerState sampler))
            {
                return sampler;
            }
            sampler = device.CreateSamplerState(new SamplerDescription(Filter.MinMagPointMipLinear, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp));
            samplerStates[samplerState] = sampler;
            return sampler;
        }

        public Vector2 m_d3dRenderTargetSize;
        public Vector2 m_outputSize;
        Vector2 m_logicalSize;

        const int c_frameCount = 2;
        public IDXGIDevice3 dxgiDevice3;
        public IDXGIAdapter dxgiAdapter;
        public IDXGIFactory4 dxgiFactory4;
        public ID3D11Device5 device;
        public ID3D11DeviceContext3 d3dContext;
        public IDXGISwapChain3 swapChain;
        public ID3D11RenderTargetView1 renderTargetView1;
        public ID3D11DepthStencilView depthStencilView;
        public Format swapChainFormat = Format.R8G8B8A8_UNorm;
        public SwapChainFlags swapChainFlags = SwapChainFlags.AllowTearing;

        public Dictionary<SamplerState, ID3D11SamplerState> samplerStates = new Dictionary<SamplerState, ID3D11SamplerState>();

        public IntPtr hwnd;
    }
}
