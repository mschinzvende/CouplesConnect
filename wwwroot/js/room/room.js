import SignalManager from "./signalManager.js";
import NotesManager from "./notes.js";
import AcsCallManager from "./acsCallManager.js";

const { sessionId, isCounselor, returnUrl, feedbackUrl, azureCallToken, azureCallGroupId, displayName } = window.roomConfig;
const waitingScreen = document.getElementById("waitingScreen");
const sessionUI = document.getElementById("sessionUI");
const statusDot = document.getElementById("statusDot");
const connectionText = document.getElementById("connectionText");
const waitingUsers = new Set();
const notes = new NotesManager(sessionId);
let alertTimer = null;
let audioContext = null;
let isExiting = false;
let isEndingSession = false;
let transcriptionActive = false;
let callConnected = false;
const receivedCaptions = new Set();

document.addEventListener("pointerdown", () => {
    audioContext ??= new AudioContext();
    audioContext.resume().catch(() => {});
}, { once: true });

// A full-page navigation tears down the browser call. Give internal links a
// native _blank target instead of relying on window.open, so browsers keep the
// room alive even when popup protections are enabled.
function isInternalNavigationLink(link) {
    if (!link || link.hasAttribute("download") || link.dataset.roomNavigation === "same-tab") return false;

    const destination = new URL(link.href, window.location.href);
    const current = new URL(window.location.href);
    const isSamePageAnchor = destination.pathname === current.pathname &&
        destination.search === current.search && destination.hash;

    return destination.origin === current.origin && !isSamePageAnchor;
}

function keepRoomOpenWhileNavigating(link) {
    if (!isInternalNavigationLink(link)) return;
    link.target = "_blank";
    link.rel = ["noopener", "noreferrer", ...link.rel.split(" ")]
        .filter((value, index, values) => value && values.indexOf(value) === index)
        .join(" ");
}

document.querySelectorAll("a[href]").forEach(keepRoomOpenWhileNavigating);
document.addEventListener("click", event => {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    keepRoomOpenWhileNavigating(event.target.closest("a[href]"));
}, true);

const call = new AcsCallManager({
    token: azureCallToken,
    groupId: azureCallGroupId,
    displayName,
    onStateChanged: state => {
        connectionText.innerText = state === "Connected" ? "In call" : `Call ${state}`;
        setTranscriptionAvailability(state === "Connected");
    },
    onRaisedHandsChanged: hands => {
        if (isCounselor && hands.length) showRoomAlert(`${hands.length} participant${hands.length === 1 ? "" : "s"} raised a hand.`);
    },
    onVideoStateChanged: enabled => setControlState("btnCam", enabled, "Turn camera off", "Turn camera on"),
    onMuteStateChanged: enabled => setControlState("btnMic", enabled, "Mute microphone", "Unmute microphone"),
    onScreenSharingChanged: sharing => setControlState("btnShare", sharing, "Stop sharing", "Share screen", "is-active"),
    onCaptionReceived: caption => {
        if (!isCounselor || caption.resultType !== "Final" || !caption.spokenText?.trim()) return;

        const speaker = caption.speaker?.displayName?.trim() || "Participant";
        const key = `${caption.timestamp instanceof Date ? caption.timestamp.toISOString() : caption.timestamp}|${speaker}|${caption.spokenText}`;
        if (receivedCaptions.has(key)) return;
        receivedCaptions.add(key);
        notes.appendTranscript({ text: caption.spokenText, speaker, timestamp: caption.timestamp });
    },
    onCaptionsStateChanged: active => setTranscriptionState(active)
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
signal.onSessionEnded = () => {
    if (!isEndingSession) exitRoom(isCounselor ? returnUrl : feedbackUrl);
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
        button.disabled = id === "transcriptionButton" ? !callConnected : false;
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
window.toggleTranscription = () => runControl("transcriptionButton", async () => {
    if (!transcriptionActive) {
        const consented = confirm("Confirm that every participant has agreed to live transcription. Finalized speech will be appended to the counselor's private session notes.");
        if (!consented) return;
        setTranscriptionStatus("Starting…");
        try {
            await call.startTranscription("en-US");
        } catch (error) {
            const message = error?.message?.includes("Spoken language requested is not supported")
                ? "English transcription is unavailable for this call"
                : "Could not start transcription";
            setTranscriptionStatus(message, "error");
            throw error;
        }
    } else {
        setTranscriptionStatus("Stopping…");
        try {
            await call.stopTranscription();
        } catch (error) {
            setTranscriptionStatus("Could not stop transcription", "error");
            throw error;
        }
    }
});

function setTranscriptionState(active) {
    transcriptionActive = active;
    const button = document.getElementById("transcriptionButton");
    if (button) {
        button.innerHTML = active
            ? '<span aria-hidden="true">■</span> Stop transcription'
            : '<span aria-hidden="true">🎙</span> Start transcription';
        button.setAttribute("aria-pressed", String(active));
        button.classList.toggle("btn-outline-primary", !active);
        button.classList.toggle("btn-outline-danger", active);
    }
    setTranscriptionStatus(active ? "Listening and appending finalized speech" : "Not started", active ? "active" : "");
    if (!callConnected) setTranscriptionAvailability(false);
}

function setTranscriptionAvailability(connected) {
    callConnected = connected;
    const button = document.getElementById("transcriptionButton");
    if (!button) return;

    button.disabled = !connected;
    if (!connected && !transcriptionActive) {
        button.innerHTML = '<span aria-hidden="true">🎙</span> Connecting call…';
        setTranscriptionStatus("Available when the call connects");
    } else if (connected && !transcriptionActive) {
        button.innerHTML = '<span aria-hidden="true">🎙</span> Start transcription';
        setTranscriptionStatus("Not started");
    }
}

function setTranscriptionStatus(message, state = "") {
    const status = document.getElementById("transcriptionState");
    if (!status) return;
    status.textContent = message;
    status.className = `transcription-state ${state}`;
}

async function exitRoom(destination) {
    if (isExiting) return;
    isExiting = true;

    try {
        await notes.flush();
        await Promise.allSettled([call.leave(), signal.dispose()]);
        notes.dispose();
    } finally {
        window.location.assign(destination);
    }
}

window.leaveSession = async () => {
    if (!confirm("Are you sure you want to leave this session?")) return;
    await exitRoom(isCounselor ? returnUrl : feedbackUrl);
};

window.endSession = async () => {
    if (!confirm("End this session for everyone? Participants will be asked to leave feedback.")) return;

    isEndingSession = true;
    try {
        await signal.endSession();
        await exitRoom(returnUrl);
    } catch (error) {
        console.error("[Room] Unable to end the session", error);
        alert("The session could not be ended. Please try again.");
        isEndingSession = false;
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
