// ==========================================
// Couples Connect
// Media Manager
// ==========================================

export default class MediaManager {

    constructor() {

        //-------------------------------------
        // Streams
        //-------------------------------------

        this.localStream = null;

        this.screenStream = null;

        //-------------------------------------
        // Current Tracks
        //-------------------------------------

        this.videoTrack = null;

        this.audioTrack = null;

        //-------------------------------------
        // State
        //-------------------------------------

        this.cameraEnabled = true;

        this.microphoneEnabled = true;

        this.screenSharing = false;

        //-------------------------------------
        // Callbacks
        //-------------------------------------

        this.onLocalStream = null;

        this.onScreenShareStarted = null;

        this.onScreenShareStopped = null;

    }

    // ==========================================
    // Initialize
    // ==========================================

    async initialize() {

        if (this.localStream)
            return this.localStream;

        console.log("[Media] Initializing...");

        this.localStream =
            await navigator.mediaDevices.getUserMedia({

                video: {

                    width: {
                        ideal: 1280
                    },

                    height: {
                        ideal: 720
                    },

                    frameRate: {
                        ideal: 30
                    }

                },

                audio: {

                    echoCancellation: true,

                    noiseSuppression: true,

                    autoGainControl: true

                }

            });

        //-------------------------------------

        this.videoTrack =
            this.localStream
                .getVideoTracks()[0];

        this.audioTrack =
            this.localStream
                .getAudioTracks()[0];

        //-------------------------------------

        console.log("[Media] Ready");

        if (this.onLocalStream)
            this.onLocalStream(
                this.localStream
            );

        return this.localStream;

    }

    // ==========================================
    // Local Stream
    // ==========================================

    getStream() {

        return this.localStream;

    }

    // ==========================================
    // Video Track
    // ==========================================

    getVideoTrack() {

        return this.videoTrack;

    }

    // ==========================================
    // Audio Track
    // ==========================================

    getAudioTrack() {

        return this.audioTrack;

    }

    // ==========================================
    // Toggle Camera
    // ==========================================

    toggleCamera() {

        if (!this.videoTrack)
            return false;

        this.cameraEnabled =
            !this.cameraEnabled;

        this.videoTrack.enabled =
            this.cameraEnabled;

        console.log(

            "[Media] Camera:",

            this.cameraEnabled

        );

        return this.cameraEnabled;

    }

    // ==========================================
    // Toggle Microphone
    // ==========================================

    toggleMicrophone() {

        if (!this.audioTrack)
            return false;

        this.microphoneEnabled =
            !this.microphoneEnabled;

        this.audioTrack.enabled =
            this.microphoneEnabled;

        console.log(

            "[Media] Microphone:",

            this.microphoneEnabled

        );

        return this.microphoneEnabled;

    }
    // ==========================================
    // Start Screen Share
    // ==========================================

    async startScreenShare() {

        if (this.screenSharing)
            return this.screenStream;

        console.log("[Media] Starting screen share...");

        try {

            this.screenStream =
                await navigator.mediaDevices.getDisplayMedia({

                    video: true,

                    audio: false

                });

            const screenTrack =
                this.screenStream.getVideoTracks()[0];

            this.screenSharing = true;

            //-------------------------------------

            screenTrack.onended = () => {

                this.stopScreenShare();

            };

            //-------------------------------------

            if (this.onScreenShareStarted)

                this.onScreenShareStarted(
                    screenTrack
                );

            console.log(
                "[Media] Screen sharing started"
            );

            return this.screenStream;

        }
        catch (err) {

            console.error(err);

            return null;

        }

    }

    // ==========================================
    // Stop Screen Share
    // ==========================================

    stopScreenShare() {

        if (!this.screenSharing)
            return;

        console.log(
            "[Media] Stopping screen share..."
        );

        //-------------------------------------

        if (this.screenStream) {

            this.screenStream
                .getTracks()
                .forEach(track => {

                    track.stop();

                });

            this.screenStream = null;

        }

        //-------------------------------------

        this.screenSharing = false;

        //-------------------------------------

        if (this.onScreenShareStopped)

            this.onScreenShareStopped(
                this.videoTrack
            );

    }

    // ==========================================
    // Is Sharing Screen
    // ==========================================

    isSharingScreen() {

        return this.screenSharing;

    }

    // ==========================================
    // Stop Local Media
    // ==========================================

    stop() {

        console.log(
            "[Media] Stopping local media..."
        );

        //-------------------------------------

        if (this.localStream) {

            this.localStream
                .getTracks()
                .forEach(track => {

                    track.stop();

                });

        }

        //-------------------------------------

        if (this.screenStream) {

            this.screenStream
                .getTracks()
                .forEach(track => {

                    track.stop();

                });

        }

        //-------------------------------------

        this.localStream = null;

        this.screenStream = null;

        this.videoTrack = null;

        this.audioTrack = null;

        this.screenSharing = false;

    }

    // ==========================================
    // Refresh Camera
    // ==========================================

    async refreshCamera() {

        this.stop();

        return await this.initialize();

    }


    // ==========================================
    // Mute
    // ==========================================

    mute() {

        if (!this.audioTrack)
            return;

        this.audioTrack.enabled = false;

        this.microphoneEnabled = false;

    }

    // ==========================================
    // Unmute
    // ==========================================

    unmute() {

        if (!this.audioTrack)
            return;

        this.audioTrack.enabled = true;

        this.microphoneEnabled = true;

    }

    // ==========================================
    // Camera Off
    // ==========================================

    cameraOff() {

        if (!this.videoTrack)
            return;

        this.videoTrack.enabled = false;

        this.cameraEnabled = false;

    }

    // ==========================================
    // Camera On
    // ==========================================

    cameraOn() {

        if (!this.videoTrack)
            return;

        this.videoTrack.enabled = true;

        this.cameraEnabled = true;

    }

    // ==========================================
    // Dispose
    // ==========================================

    dispose() {

        this.stop();

        this.onLocalStream = null;

        this.onScreenShareStarted = null;

        this.onScreenShareStopped = null;

    }

}

