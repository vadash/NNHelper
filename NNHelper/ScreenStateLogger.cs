using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace NNHelper
{
public class ScreenStateLogger
{
    private bool _run, _init;

    public void Start()
    {
        _run = true;
        var factory = new Factory1();
        //Get first adapter
        var adapter = factory.GetAdapter1(0);
        //Get device from adapter
        var device = new SharpDX.Direct3D11.Device(adapter);
        //Get front buffer of the adapter
        var output = adapter.GetOutput(0);
        var output1 = output.QueryInterface<Output1>();

        // Width/Height of desktop to capture
        var width = output.Description.DesktopBounds.Right;
        var height = output.Description.DesktopBounds.Bottom;

        // Region size to capture
        const int newSize = 416;

        // Create Staging texture CPU-accessible
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };
        var screenTexture = new Texture2D(device, textureDesc);

        Task.Factory.StartNew(() =>
        {
            // Duplicate the output
            using (var duplicatedOutput = output1.DuplicateOutput(device))
            {
                while (_run)
                {
                    try
                    {
                        // Try to get duplicated frame within given time is ms
                        duplicatedOutput.AcquireNextFrame(5, out _, out var screenResource);

                        // copy resource into memory that can be accessed by the CPU
                        using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                        // Get the desktop capture texture
                        var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                        // Create Drawing.Bitmap
                        using (var bitmap = new Bitmap(newSize, newSize, PixelFormat.Format32bppArgb))
                        {
                            var boundsRect = new Rectangle(0, 0, newSize, newSize);

                            // Copy pixels from screen capture Texture to GDI bitmap
                            var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                            // Start y coordinate
                            var dy = height / 2 - newSize / 2;
                            var sourcePtr = mapSource.DataPointer;
                            sourcePtr = IntPtr.Add(sourcePtr, dy * mapSource.RowPitch);
                            var destPtr = mapDest.Scan0;
                            for (var y = 0; y < newSize; y++)
                            {
                                // Start x coordinate
                                var dx = width / 2 - newSize / 2;
                                // Copy a single line 
                                Utilities.CopyMemory(destPtr, sourcePtr + dx * 4, newSize * 4);
                                // Advance pointers
                                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                            }

                            // Release source and dest locks
                            bitmap.UnlockBits(mapDest);
                            device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                            using (var ms = new MemoryStream())
                            {
                                bitmap.Save(ms, ImageFormat.Bmp);
                                ScreenRefreshed?.Invoke(this, ms.ToArray());
                                _init = true;
                            }
                        }
                        screenResource.Dispose();
                        duplicatedOutput.ReleaseFrame();
                    }
                    catch (SharpDXException e)
                    {
                        if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                        {
                            Trace.TraceError(e.Message);
                            Trace.TraceError(e.StackTrace);
                        }
                    }
                }
            }
        });
        while (!_init)
        {
        }
    }

    public void Stop()
    {
        _run = false;
    }

    public EventHandler<byte[]> ScreenRefreshed;
}
}
