import MediaManager from "./media.js";
import PeerManager from "./peerManager.js";
import SignalManager from "./signalManager.js";
import ParticipantManager from "./participantManager.js";
import NotesManager from "./notes.js";


function bindEvents() {

    console.log("Binding events...");

    const btnLeave = document.getElementById("btnLeave");

    console.log("Leave button:", btnLeave);

    btnLeave?.addEventListener("click", async () => {

        console.log("LEAVE BUTTON CLICKED");

        

        await window.leaveSession();

    });

}

// ==========================================
// Configuration
// ==========================================

const sessionId = window.roomConfig.sessionId;
const isCounselor = window.roomConfig.isCounselor;

// ==========================================
// Managers
// ==========================================

const media = new MediaManager();

const peer = new PeerManager(media, window.roomConfig.iceServers);

if (!window.roomConfig.hasTurnServer) {

    console.warn(
        "[WebRTC] No TURN server is configured. Calls may fail on restrictive networks."
    );

}

const participants = new ParticipantManager();

const signal = new SignalManager(
    sessionId,
    isCounselor,
    peer
);

const notes = new NotesManager(sessionId);

// ==========================================
// Elements
// ==========================================

const waitingScreen =
    document.getElementById("waitingScreen");

const sessionUI =
    document.getElementById("sessionUI");

const statusDot =
    document.getElementById("statusDot");

const connectionText =
    document.getElementById("connectionText");

const waitingUsers = new Set();

// ==========================================
// Media Ready
// ==========================================

media.onLocalStream = stream => {

    if (!participants.participants.has("self"))

        participants.setLocalStream(stream);

};

media.onScreenShareStarted = async track => {

    await peer.replaceVideoTrack(track);

};

media.onScreenShareStopped = async track => {

    await peer.replaceVideoTrack(track);

};

// ==========================================
// Remote Streams
// ==========================================

peer.onRemoteStream = (connectionId, stream) => {

    participants.addParticipant(
        connectionId,
        stream
    );

};

// ==========================================
// Peer Left
// ==========================================

peer.onPeerDisconnected = connectionId => {

    participants.removeParticipant(
        connectionId
    );

};

// ==========================================
// Connected
// ==========================================

signal.onConnected = () => {

    statusDot.classList.add("connected");

    connectionText.innerText =
        "Connected";

};

// ==========================================
// Disconnected
// ==========================================

signal.onDisconnected = () => {

    statusDot.classList.remove("connected");

    connectionText.innerText =
        "Reconnecting...";

};

// ==========================================
// Waiting Screen
// ==========================================

signal.onWaiting = () => {

    waitingScreen.classList.remove("d-none");

};

// ==========================================
// Admitted
// ==========================================

signal.onAdmitted = async () => {

    waitingScreen.classList.add("d-none");

    sessionUI.classList.remove("d-none");

};

// ==========================================
// Waiting User
// ==========================================

signal.onUserWaiting = connectionId => {

    waitingUsers.add(connectionId);

    renderWaitingUsers();

};

// ==========================================
// Raised Hand
// ==========================================

signal.onRaiseHand = connectionId => {

    participants.raiseHand(
        connectionId
    );

};
// ==========================================
// Render Waiting Users
// ==========================================

function renderWaitingUsers() {

    const panel =
        document.getElementById("waitingPanel");

    const container =
        document.getElementById("waitingListContainer");

    const badge =
        document.getElementById("waitingCount");

    if (!container)
        return;

    container.innerHTML = "";

    waitingUsers.forEach(connectionId => {

        const row = document.createElement("div");

        row.className =
            "d-flex justify-content-between align-items-center mb-2";

        row.innerHTML = `

            <span>Participant</span>

            <button
                class="btn btn-success btn-sm"
                data-id="${connectionId}">
                Admit
            </button>

        `;

        row.querySelector("button")
            .onclick = async () => {

                await signal.admit(connectionId);

                waitingUsers.delete(connectionId);

                renderWaitingUsers();

            };

        container.appendChild(row);

    });

    badge.innerText = waitingUsers.size;

    panel.classList.toggle(
        "d-none",
        waitingUsers.size === 0
    );

}

// ==========================================
// Global Button Functions
// ==========================================

window.toggleMute = function () {

    const enabled =
        media.toggleMicrophone();

    document
        .getElementById("btnMic")
        .classList.toggle(
            "active",
            enabled
        );

};

window.toggleVideo = function () {

    const enabled =
        media.toggleCamera();

    document
        .getElementById("btnCam")
        .classList.toggle(
            "active",
            enabled
        );

};


window.forceMute = function () {

    media.mute();

    document
        .getElementById("btnMic")
        .classList.remove("active");

};

window.forceUnmute = function () {

    media.unmute();

    document
        .getElementById("btnMic")
        .classList.add("active");

};


window.raiseHand = async function () {

    await signal.raiseHand();

};

window.leaveSession = async function () {

    console.log("LEAVE SESSION CLICKED");

    if (!confirm("Are you sure you want to leave this session?"))
        return;
    try {

        notes.dispose();

        peer.dispose();

        participants.dispose();

        media.dispose();

        await signal.dispose();

    }
    finally {

        window.location.href = window.roomConfig.returnUrl;

    }

};

window.saveNotes = async function (show = true) {

    await notes.save(show);

};


window.shareScreen = async function () {

    if (media.isSharingScreen())

        media.stopScreenShare();

    else

        await media.startScreenShare();

};

// ==========================================
// Timer
// ==========================================

const timer =
    document.getElementById("timer");

const started =
    new Date();

setInterval(() => {

    const elapsed =
        Math.floor(
            (Date.now() - started.getTime())
            / 1000
        );

    const hrs =
        String(
            Math.floor(elapsed / 3600)
        ).padStart(2, "0");

    const mins =
        String(
            Math.floor(elapsed % 3600 / 60)
        ).padStart(2, "0");

    const secs =
        String(
            elapsed % 60
        ).padStart(2, "0");

    timer.innerText =
        `${hrs}:${mins}:${secs}`;

}, 1000);

// ==========================================
// Next Session Countdown
// ==========================================

const next =
    document.getElementById(
        "nextSessionCountdown"
    );

if (next) {

    const target =
        new Date(
            next.dataset.time
        );

    setInterval(() => {

        const diff =
            target - new Date();

        if (diff <= 0) {

            next.innerText = "Now";

            return;

        }

        const hrs =
            Math.floor(diff / 3600000);

        const mins =
            Math.floor(
                (diff % 3600000)
                / 60000
            );

        next.innerText =
            `${hrs}h ${mins}m`;

    }, 30000);

}

// ==========================================
// Start Session
// ==========================================

(async () => {

    try {

        console.log(
            "[Room] Initializing..."
        );

        //----------------------------------

        await media.initialize();

        //----------------------------------

        if (isCounselor) {

            sessionUI.classList.remove(
                "d-none"
            );

        }

        //----------------------------------

        await signal.connect();

        notes.initialize();

        
        console.log(
            "[Room] Ready"
        );

    }
    catch (err) {

        console.error(err);

        alert(
            "Unable to start the session."
        );

    }

})();
