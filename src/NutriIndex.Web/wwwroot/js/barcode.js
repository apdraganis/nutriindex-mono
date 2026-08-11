window.barcodeScanner = (function () {
    let reader = null;
    let controls = null;

    async function start(videoElementId, dotNetRef) {
        await stop();

        if (!window.ZXingBrowser) {
            throw new Error("Barcode library not loaded.");
        }

        const codeReader = new ZXingBrowser.BrowserMultiFormatReader();
        reader = codeReader;

        const video = document.getElementById(videoElementId);
        controls = await codeReader.decodeFromVideoDevice(undefined, video, (result, error) => {
            if (result) {
                dotNetRef.invokeMethodAsync("OnBarcodeDetected", result.getText());
            }
        });
    }

    async function stop() {
        if (controls) {
            controls.stop();
            controls = null;
        }

        if (reader) {
            reader = null;
        }
    }

    return { start, stop };
})();
