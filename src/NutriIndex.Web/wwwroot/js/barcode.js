window.barcodeScanner = (function () {
    let reader = null;
    let controls = null;
    let detected = false;

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
        detected = false;

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
            if (!result || detected) {
                return;
            }

            detected = true;
            const barcode = result.getText();
            console.info("[NutriIndex] barcode detected", barcode);

            // Notify Blazor, then stop the camera. Do not await the .NET call
            // through product lookup — HTTP uses JS interop and nested awaits
            // can stall until another UI event (e.g. clicking Look up).
            void dotNetRef
                .invokeMethodAsync("OnBarcodeDetected", barcode)
                .catch((error) => console.error("[NutriIndex] barcode callback failed", error));
            void stop(videoElementId);
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
