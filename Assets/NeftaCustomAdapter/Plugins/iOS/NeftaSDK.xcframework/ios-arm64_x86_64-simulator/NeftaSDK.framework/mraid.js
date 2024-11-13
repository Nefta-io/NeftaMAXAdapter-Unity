var mraid = {
    _state: "default",
    _resizeObject: null,
    _exp: 0,
    _eventListeners: {"ready":[],"error":[],"sizeChange":[],"stateChange":[],"exposureChange":[],"audioVolumeChange":[],"viewableChange":[]},
    getVersion: function() { return window.MRAID_ENV.version },
    addEventListener: function(event, listener) {
        if (!this._eventListeners.hasOwnProperty(event)) return;
        this._eventListeners[event].push(listener)
    },
    removeEventListener: function(event, listener) {
        if (!this._eventListeners.hasOwnProperty(event)) return;
        this._eventListeners[event].splice(this._eventListeners[event].indexOf(listener), 1)
    },
    open: function(url) {window.webkit.messageHandlers.jsb.postMessage("open"+url)},
    close: function() {
        if (this._state === "expanded" || this._state === "resized") {
            this._state= "default"
        } else {
            this._state = "hidden";
            window.webkit.messageHandlers.jsb.postMessage("unload")
        }
        this._eventListeners["stateChange"].forEach(f => f(this._state))
    },
    unload: function() {window.webkit.messageHandlers.jsb.postMessage("unload")},
    useCustomClose() {},
    expand: function(url) {
        if (url != null) { window.location.href = url }
        this._state = "expanded";
        this._eventListeners["stateChange"].forEach(f => f("expanded"))
    },
    isViewable: function() { return this._exp > 0 },
    playVideo(url) { },
    resize: function() {
        if (this._resizeObject == null) { this._eventListeners["error"].forEach(f => f("e")); return }
        this._state = "resized";
        this._eventListeners["stateChange"].forEach(f => f("resized"))
    },
    storePicture: function() {},
    createCalendarEvent: function() {},
    supports: function() { return false },
    getPlacementType: function() { window.MRAID_ENV._t },
    getOrientationProperties: function() { return { "allowOrientationChange": false, "forceOrientation": window.MRAID_ENV._p.orientation } },
    setOrientationProperties: function () { },
    getCurrentAppOrientation: function() { return window.MRAID_ENV._o },
    getCurrentPosition: function() { return window.MRAID_ENV._p },
    getDefaultPosition: function() { return window.MRAID_ENV._p },
    getState: function() { return this._state },
    getExpandProperties: function(){return {"width":window.MRAID_ENV._p.width,"height":window.MRAID_ENV._p.height,"useCustomClose":true,"isModal": this._t == "interstitial" }},
    setExpandProperties: function(obj) { },
    useCustomClose: function(b) { },
    getMaxSize: function(){return{"width":window.MRAID_ENV._p.width,"height":window.MRAID_ENV._p.height}},
    getScreenSize: function() {return{"width":window.MRAID_ENV._sx,"height":window.MRAID_ENV._sy}},
    getResizeProperties: function() {return{"width":window.MRAID_ENV._p.width,"height":window.MRAID_ENV._p.height,"offsetX":window.MRAID_ENV._ox,"offsetY":window.MRAID_ENV._oy,"customClosePosition":false,"allowOffscreen":false}},
    setResizeProperties: function(obj) {
        var isValid = true;
        if (obj == undefined) isValid = false;
        else if (!("width" in obj && "height" in obj && "offsetX" in obj && "offsetY" in obj)) isValid = false;
        else if (obj["width"] > window.MRAID_ENV._p.width || obj["width"] < 50 || obj["height"] > window.MRAID_ENV._p.height || obj["height"] < 50) isValid = false;
        if (isValid) { this._resizeObject = obj }
        else { this._eventListeners["error"].forEach(f => f()) } },
    getLocation: function() { return { "lat": 0, "lon": 0, "type": 0, "accuracy": 0, "lastfix": 0 } },
    _ex: function(p) {
        this._exp = p;
        var vr = { "x": window.MRAID_ENV._ox, "y": window.MRAID_ENV._oy, "width": 0, "height": 0 };
        if(p>0){ vr["width"] = window.MRAID_ENV._p.width; vr["height"] = window.MRAID_ENV._p.height }
        var d = {"exposedPercentage": p, "viewport": { "width": window.MRAID_ENV._sx, "height": window.MRAID_ENV._sy }, "visibleRectangle": vr };
        this._eventListeners["exposureChange"].forEach(f => f(d))
        this._eventListeners["viewableChange"].forEach(f => f(true))
    }
};
