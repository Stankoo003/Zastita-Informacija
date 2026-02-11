// Funkcija za ispisivanje u log
function addLog(message) {
  const logArea = document.getElementById("logArea");
  const timestamp = new Date().toLocaleTimeString("sr-RS");
  logArea.innerHTML += `[${timestamp}] ${message}<br>`;
  logArea.scrollTop = logArea.scrollHeight; // Scroll na dno
}

// Test log poruka
addLog("Aplikacija pokrenuta ✅");

// Funkcija za čitanje fajla
function getSelectedFile() {
  const fileInput = document.getElementById("fileInput");

  if (!fileInput.files || fileInput.files.length === 0) {
    addLog("❌ Nije izabran fajl!");
    return null;
  }

  return fileInput.files[0];
}
let pollingInterval = null;
let fswPollingInterval = null;

function startPolling() {
  if (pollingInterval) return; // Već radi

  addLog("🔄 Praćenje statusa aktivno...");

  pollingInterval = setInterval(async () => {
    try {
      const response = await fetch("/api/server-status");
      const data = await response.json();

      if (data.messages && data.messages.length > 0) {
        console.log("[POLLING] Primljeno poruka:", data.messages.length);
        data.messages.forEach((msg) => addLog(msg));
      }
    } catch (error) {
      console.error("[POLLING] Greška:", error);
    }
  }, 500);
}

function stopPolling() {
  if (pollingInterval) {
    clearInterval(pollingInterval);
    pollingInterval = null;
    addLog("⏹️ Praćenje statusa zaustavljeno");
  }
}
function startFSWPolling() {
  if (fswPollingInterval) return; // Već radi

  addLog("🔄 FSW praćenje aktivno...");

  fswPollingInterval = setInterval(async () => {
    try {
      const response = await fetch("/api/fsw-status");
      const data = await response.json();

      if (data.messages && data.messages.length > 0) {
        data.messages.forEach((msg) => addLog(msg));
      }
    } catch (error) {
      console.error("[FSW POLLING] Greška:", error);
    }
  }, 500);
}

function stopFSWPolling() {
  if (fswPollingInterval) {
    clearInterval(fswPollingInterval);
    fswPollingInterval = null;
    addLog("⏹️ FSW praćenje zaustavljeno");
  }
}

// Čekaj da se stranica učita
document.addEventListener("DOMContentLoaded", function () {
  document
    .getElementById("encryptBtn")
    .addEventListener("click", async function () {
      const file = getSelectedFile();
      if (!file) return;

      const algorithm = document.getElementById("algorithmSelect").value;

      addLog(`📁 Fajl: ${file.name} (${file.size} bajtova)`);
      addLog(`🔐 Algoritam: ${algorithm}`);
      addLog("⏳ Enkriptujem...");

      // Kreiraj FormData za slanje
      const formData = new FormData();
      formData.append("file", file);
      formData.append("algorithm", algorithm);

      try {
        const response = await fetch("/api/encrypt", {
          method: "POST",
          body: formData,
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ Enkriptovano! Heš: ${result.hash.substring(0, 16)}...`);
          addLog(`📦 Veličina: ${result.size} bajtova`);
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška pri slanju: ${error.message}`);
      }
    });

  // Dugme za dekriptovanje
  document
    .getElementById("decryptBtn")
    .addEventListener("click", async function () {
      const file = getSelectedFile();
      if (!file) return;

      const algorithm = document.getElementById("algorithmSelect").value;

      addLog(`📁 Fajl: ${file.name} (${file.size} bajtova)`);
      addLog(`🔓 Dekriptujem...`);

      const formData = new FormData();
      formData.append("file", file);
      formData.append("algorithm", algorithm);

      try {
        const response = await fetch("/api/decrypt", {
          method: "POST",
          body: formData,
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ Dekriptovano! Veličina: ${result.size} bajtova`);
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška: ${error.message}`);
      }
    });

  // Dugme za pokretanje servera
  document
    .getElementById("startServerBtn")
    .addEventListener("click", async function () {
      const port = document.getElementById("portInput").value;

      addLog(`🟢 Pokrećem prijem na portu ${port}...`);

      const formData = new FormData();
      formData.append("port", port);

      try {
        const response = await fetch("/api/start-server", {
          method: "POST",
          body: formData,
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ ${result.message}`);
          addLog(`⏳ Čekam dolazne fajlove...`);
          startPolling(); // ← POKRENI POLLING
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška: ${error.message}`);
      }
    });

  document
    .getElementById("sendFileBtn")
    .addEventListener("click", async function () {
      const file = getSelectedFile();
      if (!file) return;

      const algorithm = document.getElementById("algorithmSelect").value;
      const ip = document.getElementById("ipInput").value;
      const port = document.getElementById("portInput").value;

      // DEBUG
      console.log("Algorithm:", algorithm);
      console.log("Algorithm length:", algorithm.length);
      console.log("Algorithm type:", typeof algorithm);

      addLog(`📤 Šaljem ${file.name} na ${ip}:${port}...`);
      addLog(`🔐 Algoritam: "${algorithm}"`); // Dodaj navodnike da vidiš da li je prazan

      const formData = new FormData();
      formData.append("file", file);
      formData.append("algorithm", algorithm);
      formData.append("ip", ip);
      formData.append("port", port);

      try {
        const response = await fetch("/api/send", {
          method: "POST",
          body: formData,
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ ${result.message}`);
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška: ${error.message}`);
      }
    });

  // Dugme za hashovanje
  document
    .getElementById("hashBtn")
    .addEventListener("click", async function () {
      const file = getSelectedFile();
      if (!file) return;

      addLog(`📁 Fajl: ${file.name} (${file.size} bajtova)`);
      addLog("⏳ Računam Tiger Hash...");

      const formData = new FormData();
      formData.append("file", file);

      try {
        const response = await fetch("/api/hash", {
          method: "POST",
          body: formData,
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ Tiger Hash (SHA1):`);
          addLog(`   ${result.hash}`);
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška: ${error.message}`);
      }
    });

  // Dugme za pokretanje FSW
  document
    .getElementById("startFswBtn")
    .addEventListener("click", async function () {
      const targetPath = document.getElementById("targetPathInput").value;
      const algorithm = document.getElementById("fswAlgorithmSelect").value;

      addLog(`👁️ Pokrećem FSW za folder: ${targetPath}...`);

      const formData = new FormData();
      formData.append("targetPath", targetPath);
      formData.append("algorithm", algorithm);

      try {
        const response = await fetch("/api/start-fsw", {
          method: "POST",
          body: formData,
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ ${result.message}`);
          startFSWPolling(); // Pokreni polling
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška: ${error.message}`);
      }
    });

  // Dugme za zaustavljanje FSW
  document
    .getElementById("stopFswBtn")
    .addEventListener("click", async function () {
      try {
        const response = await fetch("/api/stop-fsw", {
          method: "POST",
        });

        const result = await response.json();

        if (result.success) {
          addLog(`✅ ${result.message}`);
          stopFSWPolling();
        } else {
          addLog(`❌ Greška: ${result.error}`);
        }
      } catch (error) {
        addLog(`❌ Greška: ${error.message}`);
      }
    });
});
