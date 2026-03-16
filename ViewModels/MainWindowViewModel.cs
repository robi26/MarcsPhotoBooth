using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoBoothApp.Services;

namespace PhotoBoothApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly CameraService _cameraService;
    private bool _disposed;

    public ObservableCollection<CameraDevice> Cameras { get; } = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private Bitmap? _previewFrame;

    [ObservableProperty]
    private Bitmap? _capturedPhoto;

    [ObservableProperty]
    private bool _isCapturedPhotoVisible;

    [ObservableProperty]
    private bool _isCaptureEnabled;

    [ObservableProperty]
    private bool _isCountdownVisible;

    [ObservableProperty]
    private string _countdownText = "";

    [ObservableProperty]
    private bool _isFlashVisible;

    [ObservableProperty]
    private bool _autoPrint = true;

    [ObservableProperty]
    private string _statusText = "Select a camera to begin";

    private string? _lastSavedPath;

    public MainWindowViewModel()
    {
        _cameraService = new CameraService();
        _cameraService.FrameReady += OnFrameReady;

        // Enumerate cameras on background thread
        Task.Run(() =>
        {
            var cameras = _cameraService.EnumerateCameras();
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var cam in cameras)
                    Cameras.Add(cam);

                if (Cameras.Count > 0)
                {
                    SelectedCamera = Cameras[0];
                    StatusText = $"Found {Cameras.Count} camera(s)";
                }
                else
                {
                    StatusText = "No cameras found";
                }
            });
        });
    }

    partial void OnSelectedCameraChanged(CameraDevice? value)
    {
        if (value != null)
        {
            _cameraService.StartCapture(value.Index);
            IsCaptureEnabled = true;
            StatusText = $"Using: {value.Name}";
        }
        else
        {
            _cameraService.StopCapture();
            IsCaptureEnabled = false;
            PreviewFrame = null;
        }
    }

    private void OnFrameReady(WriteableBitmap bitmap)
    {
        PreviewFrame = bitmap;
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        IsCaptureEnabled = false;
        IsCapturedPhotoVisible = false;

        // Countdown 3 -> 2 -> 1
        for (int i = 3; i >= 1; i--)
        {
            CountdownText = i.ToString();
            IsCountdownVisible = true;
            await Task.Delay(1000);
        }

        IsCountdownVisible = false;

        // Flash effect
        IsFlashVisible = true;

        // Capture the frame
        var imageData = _cameraService.CaptureSnapshot();

        // Hide flash after brief delay
        await Task.Delay(400);
        IsFlashVisible = false;

        if (imageData != null)
        {
            // Save to disk
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            var filename = $"photobooth-{timestamp}.png";
            var fullPath = Path.Combine(picturesPath, filename);

            await File.WriteAllBytesAsync(fullPath, imageData);
            _lastSavedPath = fullPath;

            // Show captured photo
            using var ms = new MemoryStream(imageData);
            CapturedPhoto = new Bitmap(ms);
            IsCapturedPhotoVisible = true;

            StatusText = $"Saved: {filename}";

            // Open print dialog if enabled
            if (AutoPrint)
            {
                await Task.Delay(300);
                PrintService.PrintImage(fullPath);
            }
        }
        else
        {
            StatusText = "Capture failed — no frame available";
        }

        IsCaptureEnabled = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cameraService.FrameReady -= OnFrameReady;
        _cameraService.Dispose();
        GC.SuppressFinalize(this);
    }
}
