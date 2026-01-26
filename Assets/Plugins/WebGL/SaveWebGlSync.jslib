mergeInto(LibraryManager.library, {
  SaveSyncToPersistent: function() {
    if (typeof FS === 'undefined' || !FS.syncfs) {
      if (typeof SendMessage === 'function') {
        SendMessage('SaveWebGlSyncBridge', 'OnWebGlSyncToCompleted', 'WebGL syncfs is not available.');
      }
      return;
    }

    FS.syncfs(false, function(err) {
      if (err) {
        var msg = 'WebGL sync-to failed: ' + err;
        if (typeof SendMessage === 'function') {
          SendMessage('SaveWebGlSyncBridge', 'OnWebGlSyncToCompleted', msg);
        }
        return;
      }

      if (typeof SendMessage === 'function') {
        SendMessage('SaveWebGlSyncBridge', 'OnWebGlSyncToCompleted', '');
      }
    });
  },

  SaveSyncFromPersistent: function() {
    if (typeof FS === 'undefined' || !FS.syncfs) {
      if (typeof SendMessage === 'function') {
        SendMessage('SaveWebGlSyncBridge', 'OnWebGlSyncFromCompleted', 'WebGL syncfs is not available.');
      }
      return;
    }

    FS.syncfs(true, function(err) {
      if (err) {
        var msg = 'WebGL sync-from failed: ' + err;
        if (typeof SendMessage === 'function') {
          SendMessage('SaveWebGlSyncBridge', 'OnWebGlSyncFromCompleted', msg);
        }
        return;
      }

      if (typeof SendMessage === 'function') {
        SendMessage('SaveWebGlSyncBridge', 'OnWebGlSyncFromCompleted', '');
      }
    });
  }
});
