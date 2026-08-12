using Microsoft.JSInterop;

namespace NutriIndex.Web.Services;

public class BarcodeScannerService
{
    private readonly IJSRuntime _jsRuntime;

    public BarcodeScannerService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public Task StartAsync<T>(string videoElementId, DotNetObjectReference<T> dotNetReference) where T : class =>
        _jsRuntime.InvokeVoidAsync("barcodeScanner.start", videoElementId, dotNetReference).AsTask();

    public Task StopAsync() =>
        _jsRuntime.InvokeVoidAsync("barcodeScanner.stop").AsTask();
}
