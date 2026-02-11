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
    .addEventListener("click", function () {
      addLog("Kliknuto na Pokreni prijem 🟢");
    });

  // Dugme za slanje fajla
  document
    .getElementById("sendFileBtn")
    .addEventListener("click", async function () {
      const file = getSelectedFile();
      if (!file) return;

      const ip = document.getElementById("ipInput").value;
      const port = document.getElementById("portInput").value;

      addLog(`📤 Šaljem ${file.name} na ${ip}:${port}...`);

      const formData = new FormData();
      formData.append("file", file);
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
});
