// ==========================================
// Couples Connect
// Participant Manager
// ==========================================

export default class ParticipantManager {

    constructor() {

        //--------------------------------------
        // Elements
        //--------------------------------------

        this.mainVideo =
            document.getElementById("activeSpeaker");

        this.grid =
            document.getElementById("videosContainer");

        this.count =
            document.getElementById("participantCount");

        //--------------------------------------
        // State
        //--------------------------------------

        this.participants = new Map();

        this.activeParticipant = null;

        this.audioContext = null;

    }

    // ==========================================
    // Local Video
    // ==========================================

    setLocalStream(stream) {

        this.addParticipant(

            "self",

            stream,

            true

        );

        this.setActiveSpeaker("self");

    }

    // ==========================================
    // Add Participant
    // ==========================================

    addParticipant(connectionId, stream, isLocal = false) {

        //--------------------------------------

        if (this.participants.has(connectionId)) {

            const participant =
                this.participants.get(connectionId);

            participant.stream = stream;

            participant.video.srcObject = stream;

            return;

        }

        //--------------------------------------

        const wrapper =
            document.createElement("div");

        wrapper.className =
            "participant-card";

        wrapper.dataset.id =
            connectionId;

        //--------------------------------------

        const video =
            document.createElement("video");

        video.autoplay = true;

        video.playsInline = true;

        video.muted = isLocal;

        video.srcObject = stream;

        video.play()
            .catch(console.error);

        //--------------------------------------

        const footer =
            document.createElement("div");

        footer.className =
            "participant-footer";

        footer.innerHTML = `

            <span class="participant-name">

                ${isLocal ? "You" : connectionId.substring(0, 6)}

            </span>

            <div class="participant-icons">

                <span class="hand d-none">✋</span>

                <span class="mic d-none">🎤</span>

                <span class="camera d-none">📷</span>

            </div>

        `;

        //--------------------------------------

        wrapper.appendChild(video);

        wrapper.appendChild(footer);

        //--------------------------------------

        wrapper.onclick = () => {

            this.setActiveSpeaker(connectionId);

        };

        //--------------------------------------

        this.grid.appendChild(wrapper);

        //--------------------------------------

        this.participants.set(

            connectionId,

            {

                id: connectionId,

                stream,

                video,

                wrapper,

                footer,

                local: isLocal,

                speaking: false,

                muted: false,

                cameraOff: false,

                handRaised: false

            }

        );

        //--------------------------------------

        this.updateCount();

        //--------------------------------------

        this.monitorAudio(connectionId);

    }

    // ==========================================
    // Remove Participant
    // ==========================================

    removeParticipant(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.video.pause();
        participant.stream = null;

        participant.video.srcObject = null;

        participant.wrapper.remove();

        this.participants.delete(connectionId);

        //--------------------------------------

        if (this.activeParticipant === connectionId) {

            const first =
                this.participants.keys()
                    .next()
                    .value;

            if (first)

                this.setActiveSpeaker(first);

        }

        this.updateCount();

    }

    // ==========================================
    // Participant Count
    // ==========================================

    updateCount() {

        if (!this.count)
            return;

        this.count.innerText =
            this.participants.size;

    }
    // ==========================================
    // Set Active Speaker
    // ==========================================

    setActiveSpeaker(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        //--------------------------------------

        if (this.mainVideo.srcObject !== participant.stream) {

            this.mainVideo.srcObject =
                participant.stream;

            this.mainVideo
                .play()
                .catch(console.error);

        }

        //--------------------------------------

        this.activeParticipant =
            connectionId;

        //--------------------------------------

        this.participants.forEach(p => {

            p.wrapper.classList.remove(
                "active-speaker"
            );

        });

        participant.wrapper.classList.add(
            "active-speaker"
        );

    }

    // ==========================================
    // Audio Detection
    // ==========================================

    monitorAudio(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        //--------------------------------------

        if (!this.audioContext) {

            this.audioContext =
                new AudioContext();

        }

        //--------------------------------------

        const analyser =
            this.audioContext
                .createAnalyser();

        analyser.fftSize = 256;

        const source =
            this.audioContext
                .createMediaStreamSource(
                    participant.stream
                );

        source.connect(analyser);

        const buffer =
            new Uint8Array(
                analyser.frequencyBinCount
            );

        //--------------------------------------

        let lastSwitch = Date.now();

        const detect = () => {

            if (!this.participants.has(connectionId))
                return;

            analyser.getByteFrequencyData(buffer);

            const average =
                buffer.reduce(
                    (a, b) => a + b,
                    0
                ) / buffer.length;

            //----------------------------------

            if (

                average > 35 &&

                this.activeParticipant !== connectionId &&

                Date.now() - lastSwitch > 1500

            ) {

                lastSwitch = Date.now();

                this.setActiveSpeaker(
                    connectionId
                );

            }

            requestAnimationFrame(detect);

        };

        detect();

    }

    // ==========================================
    // Raise Hand
    // ==========================================

    raiseHand(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.handRaised = true;

        participant.footer
            .querySelector(".hand")
            .classList.remove("d-none");

    }

    // ==========================================
    // Lower Hand
    // ==========================================

    lowerHand(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.handRaised = false;

        participant.footer
            .querySelector(".hand")
            .classList.add("d-none");

    }

    // ==========================================
    // Camera Off
    // ==========================================

    cameraOff(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.cameraOff = true;

        participant.footer
            .querySelector(".camera")
            .classList.remove("d-none");

    }

    // ==========================================
    // Camera On
    // ==========================================

    cameraOn(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.cameraOff = false;

        participant.footer
            .querySelector(".camera")
            .classList.add("d-none");

    }

    // ==========================================
    // Muted
    // ==========================================

    mute(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.muted = true;

        participant.footer
            .querySelector(".mic")
            .classList.remove("d-none");

    }

    // ==========================================
    // Unmuted
    // ==========================================

    unmute(connectionId) {

        const participant =
            this.participants.get(connectionId);

        if (!participant)
            return;

        participant.muted = false;

        participant.footer
            .querySelector(".mic")
            .classList.add("d-none");

    }
    // ==========================================
    // Clear Participants
    // ==========================================

    clear() {

        this.participants.forEach(participant => {

            try {

                participant.video.pause();

                participant.video.srcObject = null;

                participant.wrapper.remove();

            }
            catch (err) {

                console.error(err);

            }

        });

        this.participants.clear();

        this.activeParticipant = null;

        this.updateCount();

    }

    // ==========================================
    // Dispose
    // ==========================================

    dispose() {

        this.clear();

        if (this.audioContext) {

            try {

                this.audioContext.close();

            }
            catch { }

            this.audioContext = null;

        }

    }

}

