// Ursine — the few things a WebGL build needs from the page around it. See WebBridge.cs.
//
// localStorage, not Unity's PlayerPrefs: PlayerPrefs lives in IndexedDB under a folder named for
// the page's address, and hosts such as itch.io give every upload a new address, so a new upload
// starts with nothing. localStorage is kept per site under a key we choose, so the save follows
// the game from one upload to the next.
mergeInto(LibraryManager.library, {

  UrsineStorageGet: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    var v = null;
    try { v = window.localStorage.getItem(key); } catch (e) { v = null; }
    if (v === null) return 0;
    var size = lengthBytesUTF8(v) + 1;
    var buf = _malloc(size);
    stringToUTF8(v, buf, size);
    return buf;
  },

  UrsineStorageSet: function (keyPtr, valuePtr) {
    try { window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr)); return 1; }
    catch (e) { return 0; }
  },

  UrsineStorageRemove: function (keyPtr) {
    try { window.localStorage.removeItem(UTF8ToString(keyPtr)); return 1; } catch (e) { return 0; }
  },

  // Hands the player a text file, the way a download link does.
  UrsineDownload: function (namePtr, textPtr) {
    try {
      var blob = new Blob([UTF8ToString(textPtr)], { type: "application/json" });
      var url = URL.createObjectURL(blob);
      var a = document.createElement("a");
      a.href = url;
      a.download = UTF8ToString(namePtr);
      a.style.display = "none";
      document.body.appendChild(a);
      a.click();
      setTimeout(function () { document.body.removeChild(a); URL.revokeObjectURL(url); }, 1000);
      return 1;
    } catch (e) { return 0; }
  },

  // Opens the browser's file picker. What was chosen comes back through SendMessage to the
  // named object: its text to okMethod, or an empty string to okMethod if nothing was chosen
  // and the picker reports it (not every browser does).
  UrsinePickFile: function (objPtr, okMethodPtr, acceptPtr) {
    var obj = UTF8ToString(objPtr), ok = UTF8ToString(okMethodPtr), accept = UTF8ToString(acceptPtr);
    try {
      var input = document.createElement("input");
      input.type = "file";
      if (accept) input.accept = accept;
      input.style.display = "none";
      input.onchange = function () {
        var f = input.files && input.files[0];
        if (!f) { SendMessage(obj, ok, ""); return; }
        var r = new FileReader();
        r.onload = function () { SendMessage(obj, ok, String(r.result || "")); };
        r.onerror = function () { SendMessage(obj, ok, ""); };
        r.readAsText(f);
        if (input.parentNode) input.parentNode.removeChild(input);
      };
      input.oncancel = function () { SendMessage(obj, ok, ""); if (input.parentNode) input.parentNode.removeChild(input); };
      document.body.appendChild(input);
      input.click();
      return 1;
    } catch (e) { return 0; }
  }
});
