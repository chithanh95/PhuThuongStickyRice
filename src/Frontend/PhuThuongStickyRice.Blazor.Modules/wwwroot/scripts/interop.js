var interop = interop || {};

interop.focusElementById = function (id) {
    setTimeout(function () {
        var element = document.getElementById(id);
        if (element) {
            element.focus();
        }
    }, 200)
};

interop.downloadFile = function (fileUrl, token, fileName) {
    axios.get(fileUrl, {
        headers: {
            "Authorization": "Bearer " + token,
        },
        responseType: "blob"
    }).then((rs) => {
        const url = window.URL.createObjectURL(rs.data);
        const element = document.createElement("a");
        element.href = url;
        element.download = fileName;
        document.body.appendChild(element);
        element.click();
    });
};

interop.uploadFile = function (url, token, file, dotNetObj) {

    if (!this.selectedFile) {
        return;
    }

    const formData = new FormData();
    formData.append("formFile", this.selectedFile);
    formData.append("name", file.name);
    formData.append("description", file.description);
    formData.append("encrypted", file.encrypted);

    const promise = axios.post(url, formData, {
        headers: {
            "Authorization": "Bearer " + token,
        }
    });

    promise.then((rs) => {
        const id = rs.data.id;
        dotNetObj.invokeMethodAsync("Uploaded", id);
    });
};

interop.fileSelected = function (e) {
    this.selectedFile = e.files ? e.files[0] : null;
};
/**
 * Short notification beep using the WebAudio API (no audio asset needed).
 * @param {number} [freq=880] - tone frequency in Hz
 * @param {number} [durationMs=180] - tone duration in milliseconds
 */
interop.playUrgent = function () {
    // Distinct urgent pattern: three rising beeps for the stale-order alert.
    try {
        var AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (!AudioCtx) {
            return;
        }
        var ctx = new AudioCtx();
        [660, 880, 1100].forEach(function (freq, i) {
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.type = 'square';
            osc.frequency.value = freq;
            gain.gain.value = 0.08;
            var start = ctx.currentTime + i * 0.22;
            osc.connect(gain);
            gain.connect(ctx.destination);
            gain.gain.setValueAtTime(0.08, start);
            gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.18);
            osc.start(start);
            osc.stop(start + 0.2);
        });
        setTimeout(function () { try { ctx.close(); } catch (e) { } }, 900);
    } catch (e) {
        console.warn('playUrgent failed', e);
    }
};

/**
 * Loads the reCAPTCHA v3 script once and executes it for the "checkout"
 * action, resolving with the token (or null when misconfigured).
 * @param {string} siteKey
 */
interop.recaptchaToken = function (siteKey) {
    return new Promise(function (resolve) {
        if (!siteKey) {
            resolve(null);
            return;
        }
        function execute() {
            try {
                window.grecaptcha.ready(function () {
                    window.grecaptcha.execute(siteKey, { action: 'checkout' })
                        .then(function (t) { resolve(t); }, function () { resolve(null); });
                });
            } catch (e) {
                resolve(null);
            }
        }
        if (window.grecaptcha && window.grecaptcha.execute) {
            execute();
            return;
        }
        var s = document.createElement('script');
        s.src = 'https://www.google.com/recaptcha/api.js?render=' + encodeURIComponent(siteKey);
        s.async = true;
        s.defer = true;
        s.onload = execute;
        s.onerror = function () { resolve(null); };
        document.head.appendChild(s);
    });
};
interop.playBeep = function (freq, durationMs) {
    try {
        freq = freq || 880;
        durationMs = durationMs || 180;
        var AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (!AudioCtx) {
            return;
        }
        var ctx = new AudioCtx();
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.type = 'sine';
        osc.frequency.value = freq;
        gain.gain.value = 0.15;
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        // Gentle fade-out so it doesn't click.
        gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + durationMs / 1000);
        osc.stop(ctx.currentTime + durationMs / 1000);
        osc.onended = function () { ctx.close(); };
    } catch (e) {
        console.warn('playBeep failed', e);
    }
};

/**
 * Compress a file input (from a Blazor InputFileChangeEventArgs) client-side
 * using a canvas, then hand the compressed Blob back to .NET.
 * JPEG unless the original is PNG or GIF (which keep their type).
 * NOTE: Blazor .NET 6+ can't hand a File straight to JS via dotnet ref in all
 * versions, so callers must pass the file from the InputFile oninputfile event
 * object: e.target.files[0]. Returns a Promise<Blob>.
 *
 * @param {File} file
 * @param {number} [maxDim=1600]
 * @param {number} [quality=0.85]
 */
interop.compressImage = function (file, maxDim, quality) {
    maxDim = maxDim || 1600;
    quality = quality || 0.85;
    return new Promise(function (resolve, reject) {
        if (!file) {
            reject(new Error('No file provided'));
            return;
        }
        var keepType = (file.type === 'image/png' || file.type === 'image/gif')
            ? file.type
            : 'image/jpeg';
        var img = new Image();
        var url = URL.createObjectURL(file);
        img.onload = function () {
            URL.revokeObjectURL(url);
            var scale = Math.min(1, maxDim / Math.max(img.width, img.height));
            var w = Math.round(img.width * scale);
            var h = Math.round(img.height * scale);
            var canvas = document.createElement('canvas');
            canvas.width = w;
            canvas.height = h;
            var ctx = canvas.getContext('2d');
            if (keepType === 'image/jpeg') {
                ctx.fillStyle = '#ffffff';
                ctx.fillRect(0, 0, w, h);
            }
            ctx.drawImage(img, 0, 0, w, h);
            canvas.toBlob(function (blob) {
                if (blob) {
                    resolve(blob);
                } else {
                    reject(new Error('Canvas compression failed'));
                }
            }, keepType, quality);
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            reject(new Error('Cannot decode image'));
        };
        img.src = url;
    });
};

/**
 * Compress the file chosen in an <input type="file"> (by element id) and hand
 * the bytes back to Blazor as a byte[]. GIF files are returned untouched
 * (to preserve animation). PNG keeps PNG, everything else becomes JPEG.
 * The compressed file name is also returned so the caller can keep the right
 * extension (e.g. "photo.png" -> "photo.jpg").
 *
 * @param {string} elementId - id of the <input type="file">
 * @param {number} [maxDim=1600]
 * @param {number} [quality=0.85]
 * @returns {Promise<{bytes: number[], fileName: string, contentType: string}>}
 */
interop.compressImageFromInput = function (elementId, maxDim, quality) {
    return new Promise(function (resolve, reject) {
        var input = document.getElementById(elementId);
        var file = input && input.files && input.files[0];
        if (!file) {
            reject(new Error('No file selected'));
            return;
        }
        // Preserve animated GIFs as-is.
        if (file.type === 'image/gif' || /\.gif$/i.test(file.name)) {
            readAsBytes(file, file.name, file.type || 'image/gif', resolve, reject);
            return;
        }
        interop.compressImage(file, maxDim, quality).then(function (blob) {
            var ext = blob.type === 'image/png' ? '.png' : '.jpg';
            var base = file.name.replace(/\.[^.]+$/, '') || 'image';
            var contentType = blob.type || 'image/jpeg';
            readAsBytes(blob, base + ext, contentType, resolve, reject);
        }).catch(function (err) {
            // If canvas compression fails, fall back to the original bytes.
            readAsBytes(file, file.name, file.type || 'image/jpeg', resolve, reject);
        });
    });

    function readAsBytes(blob, fileName, contentType, resolve, reject) {
        var reader = new FileReader();
        reader.onload = function () {
            var buffer = reader.result;
            var bytes = new Uint8Array(buffer);
            var jsArray = Array.prototype.slice.call(bytes);
            resolve({ bytes: jsArray, fileName: fileName, contentType: contentType });
        };
        reader.onerror = function () { reject(new Error('Cannot read file')); };
        reader.readAsArrayBuffer(blob);
    }
};

/**
 * Wave 6d (item 40): client-side error reporting. window.onerror and
 * unhandledrejection are throttled to max 1 POST per 30s per page and
 * sent to POST api/system/client-errors (anonymous, server rate-limited).
 * Entries are written to AppLogs with Source="client-js".
 * Never logs request bodies — only the message, page URL, stack and UA.
 */
(function () {
    var lastReportAt = 0;
    var THROTTLE_MS = 30000;

    function report(message, pageUrl, stack) {
        var now = Date.now();
        if (now - lastReportAt < THROTTLE_MS) {
            return;
        }
        lastReportAt = now;

        try {
            var payload = {
                Message: String(message || 'Unknown JS error').substring(0, 2000),
                PageUrl: String(pageUrl || location.href).substring(0, 500),
                StackTrace: stack ? String(stack).substring(0, 4000) : null,
                UserAgent: (navigator.userAgent || '').substring(0, 300)
            };
            fetch('api/system/client-errors', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                keepalive: true
            }).catch(function () { /* reporting must never break the page */ });
        } catch (e) {
            // Reporting must never break the page.
        }
    }

    window.addEventListener('error', function (event) {
        var err = event.error;
        report(event.message || (err && err.message) || 'JS error',
            location.href, err && err.stack);
    });

    window.addEventListener('unhandledrejection', function (event) {
        var reason = event.reason;
        var msg = (reason && reason.message) ? reason.message : String(reason);
        report(msg || 'Unhandled promise rejection', location.href,
            reason && reason.stack);
    });
})();
