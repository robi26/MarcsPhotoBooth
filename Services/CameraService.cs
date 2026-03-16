using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using OpenCvSharp;

namespace PhotoBoothApp.Services;

public record CameraDevice(int Index, string Name);

public class CameraService : IDisposable
{
    private VideoCapture? _capture;
    private CancellationTokenSource? _cts;
    private Mat? _currentFrame;
    private WriteableBitmap? _writeableBitmap;
    private readonly object _frameLock = new();
    private bool _disposed;

    public event Action<WriteableBitmap>? FrameReady;

    public List<CameraDevice> EnumerateCameras()
    {
        var cameras = new List<CameraDevice>();

        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var cap = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                if (cap.IsOpened())
                {
                    cameras.Add(new CameraDevice(i, $"Camera {i}"));
                }
            }
            catch
            {
                // Skip unavailable devices
            }
        }

        return cameras;
    }

    public void StartCapture(int deviceIndex)
    {
        StopCapture();

        _capture = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW);

        if (!_capture.IsOpened())
        {
            _capture.Dispose();
            _capture = null;
            return;
        }

        _capture.Set(VideoCaptureProperties.FrameWidth, 1920);
        _capture.Set(VideoCaptureProperties.FrameHeight, 1080);

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(() => CaptureLoop(token), token);
    }

    private void CaptureLoop(CancellationToken token)
    {
        using var mat = new Mat();

        while (!token.IsCancellationRequested)
        {
            if (_capture == null || !_capture.IsOpened())
                break;

            if (!_capture.Read(mat) || mat.Empty())
            {
                Thread.Sleep(10);
                continue;
            }

            // Convert BGR to BGRA for Avalonia
            using var bgra = new Mat();
            Cv2.CvtColor(mat, bgra, ColorConversionCodes.BGR2BGRA);

            // Store current frame for snapshot
            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = mat.Clone();
            }

            // Update WriteableBitmap on UI thread
            var width = bgra.Width;
            var height = bgra.Height;
            var data = new byte[bgra.Total() * bgra.ElemSize()];
            System.Runtime.InteropServices.Marshal.Copy(bgra.Data, data, 0, data.Length);

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (token.IsCancellationRequested) return;

                    if (_writeableBitmap == null ||
                        (int)_writeableBitmap.Size.Width != width ||
                        (int)_writeableBitmap.Size.Height != height)
                    {
                        _writeableBitmap = new WriteableBitmap(
                            new PixelSize(width, height),
                            new Vector(96, 96),
                            Avalonia.Platform.PixelFormat.Bgra8888,
                            AlphaFormat.Premul);
                    }

                    using var fb = _writeableBitmap.Lock();
                    System.Runtime.InteropServices.Marshal.Copy(data, 0, fb.Address, data.Length);

                    FrameReady?.Invoke(_writeableBitmap);
                }
                catch
                {
                    // Ignore frame errors during shutdown
                }
            });

            // Target ~30fps
            Thread.Sleep(33);
        }
    }

    public byte[]? CaptureSnapshot()
    {
        lock (_frameLock)
        {
            if (_currentFrame == null || _currentFrame.Empty())
                return null;

            Cv2.ImEncode(".png", _currentFrame, out var buf);
            return buf;
        }
    }

    public void StopCapture()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _capture?.Release();
        _capture?.Dispose();
        _capture = null;

        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopCapture();
        GC.SuppressFinalize(this);
    }
}
