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
            // Trả về base64 để Blazor deserialize thành byte[] được
            var base64 = reader.result.split(',')[1];
            resolve({ base64: base64, fileName: fileName, contentType: contentType });
        };
        reader.onerror = function () { reject(new Error('Cannot read file')); };
        reader.readAsDataURL(blob);
    }
};

/**
 * Item 166: compress ALL files chosen in an <input type="file" multiple>
 * (by element id) and hand the bytes back to Blazor as an array of
 * {bytes, fileName, contentType}. Same per-file rules as
 * compressImageFromInput (GIFs untouched, PNG kept, rest -> JPEG).
 *
 * @param {string} elementId - id of the <input type="file" multiple>
 * @param {number} [maxDim=1600]
 * @param {number} [quality=0.85]
 * @returns {Promise<Array<{bytes: number[], fileName: string, contentType: string}>>}
 */
interop.compressImagesFromInput = function (elementId, maxDim, quality) {
    var input = document.getElementById(elementId);
    var files = (input && input.files) ? Array.prototype.slice.call(input.files) : [];
    if (files.length === 0) {
        return Promise.resolve([]);
    }
    // Clear the input so the same files can be picked again.
    input.value = '';
    var jobs = files.map(function (file) {
        if (file.type === 'image/gif' || /\.gif$/i.test(file.name)) {
            return readOne(file, file.name, file.type || 'image/gif');
        }
        return interop.compressImage(file, maxDim, quality).then(function (blob) {
            var ext = blob.type === 'image/png' ? '.png' : '.jpg';
            var base = file.name.replace(/\.[^.]+$/, '') || 'image';
            return readOne(blob, base + ext, blob.type || 'image/jpeg');
        }).catch(function () {
            return readOne(file, file.name, file.type || 'image/jpeg');
        });
    });
    return Promise.all(jobs);

    function readOne(blob, fileName, contentType) {
        return new Promise(function (resolve, reject) {
            var reader = new FileReader();
            reader.onload = function () {
                var base64 = reader.result.split(',')[1];
                resolve({
                    base64: base64,
                    fileName: fileName,
                    contentType: contentType
                });
            };
            reader.onerror = function () { reject(new Error('Cannot read file')); };
            reader.readAsDataURL(blob);
        });
    }
};

/**
 * Item 141: keyboard shortcuts for the admin orders page (/orders).
 *
 * start(dotNetRef) attaches a document keydown listener that forwards
 * actions to the Blazor component via OnShortcutKey(action). stop() removes
 * the listener (called when leaving the page).
 *
 * Typing inside inputs/textareas/selects/contenteditable is ignored
 * (Escape blurs the field instead). On buttons/links only harmless
 * navigation keys (j/k/arrows, /, ?) work — Enter and letter actions are
 * left to the control itself so nothing fires twice.
 */
interop.orderShortcuts = (function () {
    var dotNetRef = null;
    var handler = null;

    function send(action) {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnShortcutKey', action);
        }
    }

    return {
        start: function (ref) {
            dotNetRef = ref;
            if (handler) {
                document.removeEventListener('keydown', handler);
            }
            handler = function (e) {
                var t = e.target;
                var tag = t && t.tagName ? t.tagName.toUpperCase() : '';
                var inTextField = tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT'
                    || (t && t.isContentEditable);
                if (inTextField) {
                    if (e.key === 'Escape' && t.blur) {
                        t.blur();
                    }
                    return;
                }
                var key = e.key;
                if (key === '?') {
                    send('help');
                    return;
                }
                if (key === '/') {
                    send('search');
                    e.preventDefault();
                    return;
                }
                if (key === 'j' || key === 'J' || key === 'ArrowDown') {
                    send('next');
                    e.preventDefault();
                    return;
                }
                if (key === 'k' || key === 'K' || key === 'ArrowUp') {
                    send('prev');
                    e.preventDefault();
                    return;
                }
                // On buttons/links, leave Enter and letters to the control.
                if (tag === 'BUTTON' || tag === 'A') {
                    return;
                }
                if (key === 'Enter') {
                    send('open');
                    return;
                }
                var lower = (key && key.length === 1) ? key.toLowerCase() : '';
                if (lower === 'c') {
                    send('confirm');
                } else if (lower === 'p') {
                    send('prepare');
                } else if (lower === 'x') {
                    send('cancel');
                }
            };
            document.addEventListener('keydown', handler);
        },
        stop: function () {
            if (handler) {
                document.removeEventListener('keydown', handler);
                handler = null;
            }
            dotNetRef = null;
        }
    };
})();
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

/* Item 160: đọc báo cáo doanh thu bằng giọng nói (Web Speech API). */
interop.speak = function (text) {
    try {
        if (!('speechSynthesis' in window)) return false;
        window.speechSynthesis.cancel();
        var utter = new SpeechSynthesisUtterance(text);
        utter.lang = 'vi-VN';
        utter.rate = 0.95;
        var voices = window.speechSynthesis.getVoices();
        var vi = voices.filter(function (v) { return v.lang && v.lang.toLowerCase().indexOf('vi') === 0; });
        if (vi.length > 0) utter.voice = vi[0];
        window.speechSynthesis.speak(utter);
        return true;
    } catch (e) { return false; }
};

interop.stopSpeak = function () {
    try { if ('speechSynthesis' in window) window.speechSynthesis.cancel(); } catch (e) { }
};

/* Parallax mượt cho banner CTA landing: dùng rAF + transform (GPU), không gây giật như background-attachment:fixed. */
(function () {
    var ticking = false;
    var cta = null;
    var bg = null;

    function update() {
        ticking = false;
        if (!cta || !bg) return;
        var rect = cta.getBoundingClientRect();
        var vh = window.innerHeight || 800;
        // Chỉ tính khi banner trong/kề viewport.
        if (rect.bottom < -200 || rect.top > vh + 200) return;
        // Dịch nền chậm hơn tốc độ cuộn (parallax 0.25).
        var progress = (rect.top + rect.height / 2 - vh / 2) / (vh / 2 + rect.height / 2);
        var shift = Math.max(-1, Math.min(1, progress)) * rect.height * 0.18;
        bg.style.transform = 'translate3d(0,' + shift.toFixed(1) + 'px,0)';
    }

    function onScroll() {
        if (!ticking) {
            ticking = true;
            requestAnimationFrame(update);
        }
    }

    window.ptInitCtaParallax = function () {
        cta = document.querySelector('.sg-cta');
        bg = cta ? cta.querySelector('.sg-cta-bg') : null;
        if (!cta || !bg) return;
        if (!window.__ptParallaxBound) {
            window.__ptParallaxBound = true;
            window.addEventListener('scroll', onScroll, { passive: true });
            window.addEventListener('resize', onScroll, { passive: true });
        }
        update();
    };
})();

/* Hiệu ứng hiện dần khi cuộn tới (IntersectionObserver) cho landing. */
window.ptInitReveal = function () {
    try {
        var els = document.querySelectorAll('.sg-reveal');
        if (!('IntersectionObserver' in window) || els.length === 0) {
            els.forEach(function (el) { el.classList.add('in'); });
            return;
        }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) {
                    e.target.classList.add('in');
                    io.unobserve(e.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });
        els.forEach(function (el) { io.observe(el); });
    } catch (e) { }
};

/* Landing item 4: đếm ngược khuyến mãi. */
window.ptInitCountdown = function () {
    try {
        var el = document.querySelector('.sg-countdown-timer');
        if (!el || el.dataset.done) return;
        el.dataset.done = "1";
        var end = new Date(el.dataset.end).getTime();
        if (isNaN(end)) return;
        var dEl = el.querySelector('[data-cd="d"]');
        var hEl = el.querySelector('[data-cd="h"]');
        var mEl = el.querySelector('[data-cd="m"]');
        var sEl = el.querySelector('[data-cd="s"]');
        function pad(n) { return (n < 10 ? "0" : "") + n; }
        function tick() {
            var diff = end - Date.now();
            if (diff <= 0) {
                var bar = el.closest('.sg-countdown-bar');
                if (bar) bar.style.display = "none";
                return;
            }
            var d = Math.floor(diff / 86400000);
            var h = Math.floor(diff % 86400000 / 3600000);
            var m = Math.floor(diff % 3600000 / 60000);
            var s = Math.floor(diff % 60000 / 1000);
            if (dEl) dEl.textContent = d;
            if (hEl) hEl.textContent = pad(h);
            if (mEl) mEl.textContent = pad(m);
            if (sEl) sEl.textContent = pad(s);
            setTimeout(tick, 1000);
        }
        tick();
    } catch (e) { }
};

/* Landing item 18: nạp Facebook Pixel + Google Analytics khi có ID (cấu hình trong Cài đặt). */
window.ptInitTracking = function (pixelId, gaId) {
    try {
        if (pixelId && !window.__ptPixelLoaded) {
            window.__ptPixelLoaded = true;
            !function (f, b, e, v, n, t, s) {
                if (f.fbq) return; n = f.fbq = function () {
                    n.callMethod ? n.callMethod.apply(n, arguments) : n.queue.push(arguments);
                };
                if (!f._fbq) f._fbq = n; n.push = n; n.loaded = !0; n.version = '2.0';
                n.queue = []; t = b.createElement(e); t.async = !0;
                t.src = v; s = b.getElementsByTagName(e)[0];
                s.parentNode.insertBefore(t, s);
            }(window, document, 'script', 'https://connect.facebook.net/en_US/fbevents.js');
            fbq('init', pixelId);
            fbq('track', 'PageView');
        }
        if (gaId && !window.__ptGaLoaded) {
            window.__ptGaLoaded = true;
            var s = document.createElement('script');
            s.async = true;
            s.src = 'https://www.googletagmanager.com/gtag/js?id=' + encodeURIComponent(gaId);
            document.head.appendChild(s);
            window.dataLayer = window.dataLayer || [];
            window.gtag = function () { dataLayer.push(arguments); };
            gtag('js', new Date());
            gtag('config', gaId);
        }
    } catch (e) { }
};

/* Landing item 26: banner khi trời Hà Nội lạnh (<20°C) — Open-Meteo miễn phí, không cần key. */
window.ptCheckColdWeather = function () {
    try {
        return fetch("https://api.open-meteo.com/v1/forecast?latitude=21.03&longitude=105.85&current=temperature_2m&timezone=Asia%2FBangkok")
            .then(function (r) { return r.json(); })
            .then(function (j) {
                var t = j && j.current ? j.current.temperature_2m : null;
                return t !== null && t < 20 ? String(Math.round(t)) : "";
            })
            .catch(function () { return ""; });
    } catch (e) { return Promise.resolve(""); }
};

/* Landing item 24: món đã xem (localStorage). */
window.ptGetRecentViews = function () {
    try { return JSON.parse(localStorage.getItem("pt-recent-views") || "[]"); }
    catch (e) { return []; }
};
window.ptPushRecentView = function (id) {
    try {
        var arr = window.ptGetRecentViews().filter(function (x) { return x !== id; });
        arr.unshift(id);
        localStorage.setItem("pt-recent-views", JSON.stringify(arr.slice(0, 6)));
    } catch (e) { }
};

/* Landing item 27: chia sẻ. */
window.ptShare = function (title, text, url) {
    try {
        if (navigator.share) {
            navigator.share({ title: title, text: text, url: url }).catch(function () { });
            return "shared";
        }
        if (navigator.clipboard) {
            navigator.clipboard.writeText(url).catch(function () { });
            return "copied";
        }
        return "none";
    } catch (e) { return "none"; }
};

/* Landing item 31: in/tải menu PDF. */
window.ptPrintMenu = function () { try { window.print(); } catch (e) { } };

/* Items 66/80: nhập giọng nói (Web Speech API). */
window.ptStartVoiceInput = function () {
    return new Promise(function (resolve) {
        try {
            var SR = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SR) { resolve(""); return; }
            var rec = new SR();
            rec.lang = "vi-VN";
            rec.interimResults = false;
            rec.maxAlternatives = 1;
            rec.onresult = function (e) {
                try { resolve(e.results[0][0].transcript || ""); } catch (err) { resolve(""); }
            };
            rec.onerror = function () { resolve(""); };
            rec.onend = function () { resolve(""); };
            // Tránh resolve 2 lần
            var done = false;
            var once = function (t) { if (!done) { done = true; resolve(t); } try { rec.stop(); } catch (e) {} };
            rec.onresult = function (e) { try { once(e.results[0][0].transcript || ""); } catch (err) { once(""); } };
            rec.onerror = function () { once(""); };
            setTimeout(function () { once(""); }, 15000);
            rec.start();
        } catch (e) { resolve(""); }
    });
};

/* Export CSV/PDF: download file from API with bearer token. */
interop.downloadFile = function (url, token, fileName) {
    return fetch(url, {
        headers: token ? { "Authorization": "Bearer " + token } : {}
    })
    .then(function (resp) {
        if (!resp.ok) { throw new Error("Download failed: " + resp.status); }
        return resp.blob();
    })
    .then(function (blob) {
        var a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = fileName || "download";
        document.body.appendChild(a);
        a.click();
        setTimeout(function () {
            URL.revokeObjectURL(a.href);
            a.remove();
        }, 1000);
    })
    .catch(function (err) {
        console.error("downloadFile error:", err);
        alert("Không tải được file: " + err.message);
    });
};
