mergeInto(LibraryManager.library, {

  WebGLCopyToClipboard: function (text) {
    navigator.clipboard.writeText(Pointer_stringify(text));
  },        


  WebGLRequestClipboardPaste: function (objectName) {
    var strObjectName = Pointer_stringify(objectName);
    navigator.clipboard.readText().then(function(text){
        unityInstance.SendMessage(strObjectName, "WebGLReceiveClipboardPaste", text);
        });
  },

});