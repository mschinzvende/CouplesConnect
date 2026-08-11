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
let alertTimer = null;
let audioContext = null;

document.addEventListener("pointerdown", () => {
    audioContext ??= new AudioContext();
    audioContext.resume().catch(() => {});
}, { once: true });

const call = new AcsCallManager({
    token: azureCallToken,
    groupId: azureCallGroupId,
    onStateChanged: state => {
        connectionText.innerText = state === "Connected" ? "In call" : `Call ${state}`;
    },
    onRaisedHandsChanged: hands => {
        if (isCounselor && hands.length) showRoomAlert(`${hands.length} participant${hands.length === 1 ? "" : "s"} raised a hand.`);
    },
    onVideoStateChanged: enabled => setControlState("btnCam", enabled, "Turn camera off", "Turn camera on"),
    onMuteStateChanged: enabled => setControlState("btnMic", enabled, "Mute microphone", "Unmute microphone"),
    onScreenSharingChanged: sharing => setControlState("btnShare", sharing, "Stop sharing", "Share screen", "is-active")
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
    showRoomAlert("A participant is waiting to be admitted.");
};

function showRoomAlert(message) {
    if (!isCounselor) return;
    const alert = document.getElementById("roomAlert");
    if (!alert) return;
    alert.textContent = message;
    alert.classList.remove("d-none");
    playNotificationTone();
    clearTimeout(alertTimer);
    alertTimer = setTimeout(() => alert.classList.add("d-none"), 7000);
}

function playNotificationTone() {
    if (!audioContext || audioContext.state !== "running") return;
    const oscillator = audioContext.createOscillator();
    const gain = audioContext.createGain();
    oscillator.frequency.setValueAtTime(740, audioContext.currentTime);
    oscillator.frequency.setValueAtTime(988, audioContext.currentTime + 0.12);
    gain.gain.setValueAtTime(0.0001, audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.12, audioContext.currentTime + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, audioContext.currentTime + 0.28);
    oscillator.connect(gain).connect(audioContext.destination);
    oscillator.start();
    oscillator.stop(audioContext.currentTime + 0.3);
}

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

async function runControl(id, action) {
    const button = document.getElementById(id);
    if (!button || button.disabled) return;
    button.disabled = true;
    try {
        await action();
    } catch (error) {
        console.error(`[Room] ${id} action failed`, error);
        alert("That control could not be updated. Please check your browser permissions and try again.");
    } finally {
        button.disabled = false;
    }
}

window.toggleMute = () => runControl("btnMic", async () => {
    const enabled = await call.toggleMute();
    setControlState("btnMic", enabled, "Mute microphone", "Unmute microphone");
});
window.toggleVideo = () => runControl("btnCam", async () => {
    const enabled = await call.toggleVideo();
    setControlState("btnCam", enabled, "Turn camera off", "Turn camera on");
});
window.shareScreen = () => runControl("btnShare", async () => {
    const sharing = await call.toggleScreenShare();
    setControlState("btnShare", sharing, "Stop sharing", "Share screen", "is-active");
});
window.raiseHand = () => runControl("btnHand", async () => {
    const raised = await call.toggleRaiseHand();
    setControlState("btnHand", raised, "Lower hand", "Raise hand", "is-active");
});
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

function setControlState(id, enabled, onLabel, offLabel, activeClass = "is-on") {
    const button = document.getElementById(id);
    button.classList.toggle(activeClass, enabled);
    button.classList.toggle("is-off", !enabled && (id === "btnMic" || id === "btnCam"));
    button.title = enabled ? onLabel : offLabel;
    button.setAttribute("aria-label", button.title);
    button.setAttribute("aria-pressed", String(enabled));
}

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
