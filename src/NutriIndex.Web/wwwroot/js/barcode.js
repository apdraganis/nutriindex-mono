window.barcodeScanner = (function () {
    let reader = null;
    let controls = null;

    function releaseVideo(videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video?.srcObject) {
            return;
        }

        video.srcObject.getTracks().forEach((track) => track.stop());
        video.srcObject = null;
    }

    async function start(videoElementId, dotNetRef) {
        await stop(videoElementId);

        if (!window.ZXingBrowser) {
            throw new Error("Barcode scanner library failed to load. Reload the page and try again.");
        }

        if (!navigator.mediaDevices?.getUserMedia) {
            const secure = window.isSecureContext;
            throw new Error(
                secure
                    ? "Camera is not supported in this browser."
                    : "Camera requires HTTPS (or localhost). Open the app over HTTPS to scan barcodes."
            );
        }

        const video = document.getElementById(videoElementId);
        if (!video) {
            throw new Error("Camera preview is not ready yet. Try again in a moment.");
        }

        const codeReader = new ZXingBrowser.BrowserMultiFormatReader();
        reader = codeReader;

        const onResult = (result) => {
            if (result) {
                dotNetRef.invokeMethodAsync("OnBarcodeDetected", result.getText());
            }
        };

        const constraints = {
            video: {
                facingMode: { ideal: "environment" },
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: false
        };

        try {
            controls = await codeReader.decodeFromConstraints(constraints, videoElementId, onResult);
        } catch {
            controls = await codeReader.decodeFromVideoDevice(undefined, videoElementId, onResult);
        }
    }

    async function stop(videoElementId = "barcode-video") {
        if (controls) {
            controls.stop();
            controls = null;
        }

        releaseVideo(videoElementId);
        reader = null;
    }

    return { start, stop };
})();
