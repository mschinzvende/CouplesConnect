import SignalManager from "./signalManager.js";
import NotesManager from "./notes.js";
import AcsCallManager from "./acsCallManager.js";

const { sessionId, isCounselor, azureCallToken, azureCallGroupId } = window.roomConfig;
const waitingScreen = document.getElementById("waitingScreen");
const sessionUI = document.getElementById("sessionUI");
const statusDot = document.getElementById("statusDot");
const connectionText = document.getElementById("connectionText");
const waitingUsers = new Set();
const notes = new NotesManager(sessionId);

const call = new AcsCallManager({
    token: azureCallToken,
    groupId: azureCallGroupId,
    onStateChanged: state => {
        connectionText.innerText = state === "Connected" ? "In call" : `Call ${state}`;
    }
});

const signal = new SignalManager(sessionId, isCounselor);

async function joinCall() {
    try {
        await call.join();
    } catch (error) {
        console.error("[ACS] Unable to join call", error);
        alert("Unable to join the video call. Please check camera and microphone permissions.");
    }
}

signal.onConnected = () => {
    statusDot.classList.add("connected");
    connectionText.innerText = "Connected";
};
signal.onDisconnected = () => {
    statusDot.classList.remove("connected");
    connectionText.innerText = "Reconnecting...";
};
signal.onWaiting = () => waitingScreen.classList.remove("d-none");
signal.onAdmitted = async () => {
    waitingScreen.classList.add("d-none");
    sessionUI.classList.remove("d-none");
    await joinCall();
};
signal.onUserWaiting = connectionId => {
    waitingUsers.add(connectionId);
    renderWaitingUsers();
};

function renderWaitingUsers() {
    const panel = document.getElementById("waitingPanel");
    const container = document.getElementById("waitingListContainer");
    const badge = document.getElementById("waitingCount");
    if (!container) return;
    container.innerHTML = "";
    waitingUsers.forEach(connectionId => {
        const row = document.createElement("div");
        row.className = "d-flex justify-content-between align-items-center mb-2";
        row.innerHTML = '<span>Participant</span><button class="btn btn-success btn-sm">Admit</button>';
        row.querySelector("button").onclick = async () => {
            await signal.admit(connectionId);
            waitingUsers.delete(connectionId);
            renderWaitingUsers();
        };
        container.appendChild(row);
    });
    badge.innerText = waitingUsers.size;
    panel.classList.toggle("d-none", waitingUsers.size === 0);
}

window.toggleMute = async () => {
    const enabled = await call.toggleMute();
    document.getElementById("btnMic").classList.toggle("active", enabled);
};
window.toggleVideo = async () => {
    const enabled = await call.toggleVideo();
    document.getElementById("btnCam").classList.toggle("active", enabled);
};
window.shareScreen = () => alert("Screen sharing is not enabled in this first ACS Calling migration.");
window.raiseHand = async () => signal.raiseHand();
window.saveNotes = async show => notes.save(show);
window.leaveSession = async () => {
    if (!confirm("Are you sure you want to leave this session?")) return;
    try {
        await call.leave();
        await signal.dispose();
        notes.dispose();
    } finally {
        window.location.href = window.roomConfig.returnUrl;
    }
};

const started = new Date();
setInterval(() => {
    const elapsed = Math.floor((Date.now() - started.getTime()) / 1000);
    document.getElementById("timer").innerText = [
        Math.floor(elapsed / 3600),
        Math.floor(elapsed % 3600 / 60),
        elapsed % 60
    ].map(value => String(value).padStart(2, "0")).join(":");
}, 1000);

(async () => {
    try {
        if (isCounselor) sessionUI.classList.remove("d-none");
        await signal.connect();
        notes.initialize();
        if (isCounselor) await joinCall();
    } catch (error) {
        console.error(error);
        alert("Unable to start the session.");
    }
})();
