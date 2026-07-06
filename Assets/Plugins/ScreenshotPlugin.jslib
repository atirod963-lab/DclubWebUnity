mergeInto(LibraryManager.library, {
    DownloadScreenshotJS: function(byteData, byteLength, fileNamePtr) {
        var fileName = UTF8ToString(fileNamePtr);
        var data = new Uint8Array(HEAPU8.buffer, byteData, byteLength);
        var blob = new Blob([data], { type: "image/png" });
        var url = window.URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    }
});