using Microsoft.JSInterop;

namespace NutriIndex.Web.Services;

public class BarcodeScannerService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<BarcodeScannerService>? _reference;
    private Func<string, Task>? _onDetected;

    public BarcodeScannerService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task StartAsync(string videoElementId, Func<string, Task> onDetected)
    {
        _onDetected = onDetected;
        _reference = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("barcodeScanner.start", videoElementId, _reference);
    }

    public async Task StopAsync()
    {
        await _jsRuntime.InvokeVoidAsync("barcodeScanner.stop");
        _reference?.Dispose();
        _reference = null;
        _onDetected = null;
    }

    [JSInvokable]
    public async Task OnBarcodeDetected(string barcode)
    {
        if (_onDetected is not null)
            await _onDetected(barcode);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
