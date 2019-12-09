using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Windows.UI.Core;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;

using Microsoft.Graphics.Canvas;

using Org.WebRtc;

namespace Sora.Device
{
    public class Screen : IDisposable
    {
        public static readonly string DeviceId = "screen-share";
        public static readonly string DeviceName = "Screen Sharing";

        readonly CoreDispatcher dispatcher;

        private ICustomVideoCapturer customVideoCapturer;
        private SizeInt32 lastSize;
        private GraphicsCaptureItem item;
        private Direct3D11CaptureFramePool framePool;
        private GraphicsCaptureSession session;
        private CanvasDevice canvasDevice;
        private BlockingCollection<Direct3D11CaptureFrame> screenCaptureQueue = 
            new BlockingCollection<Direct3D11CaptureFrame>();
        private Task screenCaptureTask;

        public Screen(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public CustomVideoCapturerFactory CreateCapturerFactory()
        {
            var factory = CustomVideoCapturerFactory.Cast(CustomVideoCapturerFactory.Create());
            factory.OnCreateCustomVideoCapturer += VideoCapturerFactory_OnCreateCustomVideoCapturer;
            return factory;
        }

        private void VideoCapturerFactory_OnCreateCustomVideoCapturer(ICustomVideoCapturerCreateEvent ev)
        {
            var parameters = new CustomVideoCapturerParameters();
            customVideoCapturer = CustomVideoCapturer.Cast(CustomVideoCapturer.Create(parameters));
            ev.CreatedCapturer = customVideoCapturer;
        }

        public async Task StartCaptureAsync()
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                await StartCaptureAsyncInternal();
            });
        }

        public void Dispose()
        {
            StopCapture();
        }

        public void StopCapture()
        {
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                StopCaptureInternal();
            }).AsTask().Wait();
        }

        public void StopCaptureInternal()
        {
            if (item != null)
            {
                framePool.FrameArrived -= FramePool_FrameArrived;
            }

            if (null != screenCaptureTask)
            {
                screenCaptureQueue.Add(null);
                screenCaptureTask.Wait();
                screenCaptureTask = null;
            }

            session?.Dispose();
            framePool?.Dispose();
            item = null;
            session = null;
            framePool = null;
        }

        public async Task StartCaptureAsyncInternal()
        {
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            if (item == null)
            {
                return;
            }

            StopCaptureInternal();

            this.item = item;
            this.lastSize = item.Size;

            screenCaptureTask = CreateScreenCaptureTask();
            screenCaptureTask.Start();

            if (canvasDevice == null)
            {
                canvasDevice = new CanvasDevice();
            }

            framePool = Direct3D11CaptureFramePool.Create(
                canvasDevice, 
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                item.Size);

            framePool.FrameArrived += FramePool_FrameArrived;
            item.Closed += (s, a) =>
            {
                StopCaptureInternal();
            };
            session = framePool.CreateCaptureSession(item);
            session.StartCapture();
        }

        private Task CreateScreenCaptureTask()
        {
            return new Task(() => {
                if (null == customVideoCapturer)
                {
                    return;
                }

                while (true)
                {
                    using (var frame = screenCaptureQueue.Take())
                    {
                        if (null == frame)
                        {
                            return;
                        }

                        if ((frame.ContentSize.Width != lastSize.Width) 
                        || (frame.ContentSize.Height != lastSize.Height))
                        {
                            lastSize = frame.ContentSize;
                            ResetFramePool(frame.ContentSize, false);
                        }

                        try
                        {
                            var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, frame.Surface);

                            uint actualBitmapWidth = canvasBitmap.SizeInPixels.Width;
                            uint actualBitmapHeight = canvasBitmap.SizeInPixels.Height;

                            uint bitmapWidth = actualBitmapWidth;
                            uint bitmapHeight = actualBitmapHeight;

                            if (bitmapWidth % 2 != 0)
                                bitmapWidth += 1;
                            if (bitmapHeight % 2 != 0)
                                bitmapHeight += 1;

                            VideoData rgbData = new VideoData((ulong)(bitmapWidth * bitmapHeight * 4));
                            var pixels = canvasBitmap.GetPixelBytes();

                            if (bitmapWidth != actualBitmapWidth)
                            {
                                var tmpPixels = new byte[bitmapWidth * bitmapHeight * 4];
                                Int64 indexSource = 0;
                                Int64 indexDest = 0;
                                Int64 strideSource = actualBitmapWidth * 4;
                                Int64 strideDest = bitmapWidth * 4;
                                for (uint y = 0; y < actualBitmapHeight; ++y)
                                {
                                    Array.Copy(pixels, indexSource, tmpPixels, indexDest, strideSource);
                                    indexSource += strideSource;
                                    indexDest += strideDest;
                                }
                                pixels = tmpPixels;
                            }

                            rgbData.SetData8bit(pixels);

                            var buffer = VideoFrameBuffer.CreateFromARGB(
                                (int)bitmapWidth, 
                                (int)bitmapHeight, 
                                (int)(4 * bitmapWidth), 
                                rgbData);

                            customVideoCapturer.NotifyFrame(
                                buffer, 
                                (ulong)(DateTimeOffset.Now.ToUnixTimeMilliseconds()), 
                                VideoRotation.Rotation0);
                        }
                        catch (Exception e) when (canvasDevice.IsDeviceLost(e.HResult))
                        {
                            ResetFramePool(frame.ContentSize, true);
                        }
                    }
                }
            });
        }

        private void ResetFramePool(SizeInt32 size, bool recreateDevice)
        {
            do
            {
                try
                {
                    if (recreateDevice)
                    {
                        canvasDevice = new CanvasDevice();
                    }

                    framePool.Recreate(
                        canvasDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        2,
                        size);
                }
                catch (Exception e) when (canvasDevice.IsDeviceLost(e.HResult))
                {
                    canvasDevice = null;
                    recreateDevice = true;
                }

            } while (canvasDevice == null);
        }

        private void FramePool_FrameArrived(Direct3D11CaptureFramePool p, object o)
        {
            var frame = p.TryGetNextFrame();
            if (null == frame)
            {
                return;
            }

            if (screenCaptureQueue.Count > 120)
            {
                using (frame = screenCaptureQueue.Take()) { }
            }

            screenCaptureQueue.Add(frame);
        }

    }
}
