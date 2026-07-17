// ==========================================
// Couples Connect
// Peer Manager
// ==========================================

export default class PeerManager {

    constructor(mediaManager) {

        this.media = mediaManager;

        this.connection = null;

        this.peers = new Map();

        this.pendingCandidates = new Map();

        this.onRemoteStream = null;

        this.onPeerDisconnected = null;

    }

    // ==========================================
    // SignalR Connection
    // ==========================================

    setConnection(connection) {

        this.connection = connection;

    }

    // ==========================================
    // Get Peer
    // ==========================================

    getPeer(connectionId) {

        return this.peers.get(connectionId);

    }

    // ==========================================
    // Has Peer
    // ==========================================

    hasPeer(connectionId) {

        return this.peers.has(connectionId);

    }

    // ==========================================
    // Create Peer
    // ==========================================

    async createPeer(connectionId, initiator = false) {

        if (this.peers.has(connectionId)) {

            console.log(
                "[Peer] Already exists:",
                connectionId
            );

            return this.peers.get(connectionId);

        }

        console.log(
            "[Peer] Creating:",
            connectionId
        );

        const pc = new RTCPeerConnection({

            iceServers: [

                {
                    urls: [
                        "stun:stun.l.google.com:19302",
                        "stun:stun1.l.google.com:19302"
                    ]
                }

            ]

        });

        this.peers.set(connectionId, pc);

        //----------------------------------------------------
        // Local Media
        //----------------------------------------------------

        const stream = this.media.getStream();

        if (!stream)
            throw new Error(
                "Local media not initialized."
            );

        stream.getTracks().forEach(track => {

            pc.addTrack(track, stream);

        });

        //----------------------------------------------------
        // Remote Stream
        //----------------------------------------------------

        pc.ontrack = (event) => {

            console.log(
                "[Peer] Remote stream:",
                connectionId
            );

            const remoteStream = event.streams[0];

            if (!remoteStream)
                return;

            if (this.onRemoteStream)
                this.onRemoteStream(
                    connectionId,
                    remoteStream
                );

        };

        //----------------------------------------------------
        // ICE Candidate
        //----------------------------------------------------

        pc.onicecandidate = async (event) => {

            if (!event.candidate)
                return;

            console.log(
                "[ICE] Sending candidate"
            );

            await this.connection.invoke(

                "SendIceCandidate",

                connectionId,

                JSON.stringify(event.candidate)

            );

        };

        //----------------------------------------------------
        // Connection State
        //----------------------------------------------------

        pc.onconnectionstatechange = () => {

            console.log(

                "[Peer]",

                connectionId,

                pc.connectionState

            );

            switch (pc.connectionState) {

                case "connected":

                    console.log(
                        "[Peer] Connected"
                    );

                    break;

                case "disconnected":

                case "failed":

                case "closed":

                    this.removePeer(
                        connectionId
                    );

                    break;

            }

        };

        //----------------------------------------------------
        // ICE State
        //----------------------------------------------------

        pc.oniceconnectionstatechange = () => {

            console.log(

                "[ICE]",

                connectionId,

                pc.iceConnectionState

            );

        };

       

        return pc;

    }

    // ==========================================
    // Create Offer
    // ==========================================

    async createOffer(connectionId) {

        const pc = this.peers.get(connectionId);

        if (!pc)
            return;

        console.log("[Peer] Creating Offer");

        if (pc.signalingState !== "stable")
            return;

        const offer = await pc.createOffer();

        await pc.setLocalDescription(offer);

        console.log(
            "[Peer] Local offer created",
            connectionId
        );

        await this.connection.invoke(

            "SendOffer",

            connectionId,

            JSON.stringify(offer)

        );

    }
    // ==========================================
    // Receive Offer
    // ==========================================

    async receiveOffer(connectionId, offerJson) {

        console.log(
            "[Peer] Offer received:",
            connectionId
        );

        let pc = this.peers.get(connectionId);

        if (!pc)
            pc = await this.createPeer(connectionId, false);

        const offer =
            new RTCSessionDescription(
                JSON.parse(offerJson)
            );

        if (
            !pc.currentRemoteDescription &&
            pc.signalingState === "stable"
        ) {
            await pc.setRemoteDescription(offer);
        }

        //--------------------------------------------------
        // Flush queued ICE candidates
        //--------------------------------------------------

        if (this.pendingCandidates.has(connectionId)) {

            const queued =
                this.pendingCandidates.get(connectionId);

            for (const candidate of queued) {

                await pc.addIceCandidate(candidate);

            }

            this.pendingCandidates.delete(connectionId);

        }

        //--------------------------------------------------
        // Create Answer
        //--------------------------------------------------

        const answer =
            await pc.createAnswer();

        await pc.setLocalDescription(answer);

        console.log(
            "[Peer] Local answer created",
            connectionId
        );

        await this.connection.invoke(

            "SendAnswer",

            connectionId,

            JSON.stringify(answer)

        );

    }

    // ==========================================
    // Receive Answer
    // ==========================================

    async receiveAnswer(connectionId, answerJson) {

        console.log(
            "[Peer] Answer received:",
            connectionId
        );

        const pc =
            this.peers.get(connectionId);

        if (!pc)
            return;

        const answer =
            new RTCSessionDescription(
                JSON.parse(answerJson)
            );

        if (
            pc.signalingState === "have-local-offer"
        ) {
            await pc.setRemoteDescription(answer);
        }

    }

    // ==========================================
    // Receive ICE Candidate
    // ==========================================

    async receiveIceCandidate(connectionId, candidateJson) {

        const pc =
            this.peers.get(connectionId);

        const candidate =
            new RTCIceCandidate(
                JSON.parse(candidateJson)
            );

        //--------------------------------------------------
        // Peer not ready yet
        //--------------------------------------------------

        if (!pc ||
            !pc.remoteDescription) {

            if (!this.pendingCandidates.has(connectionId)) {

                this.pendingCandidates.set(

                    connectionId,

                    []

                );

            }

            this.pendingCandidates
                .get(connectionId)
                .push(candidate);

            return;

        }

        try {

            await pc.addIceCandidate(candidate);

        }
        catch (err) {

            console.error(err);

        }

    }

    // ==========================================
    // Replace Video Track
    // ==========================================

    async replaceVideoTrack(track) {

        for (const peer of this.peers.values()) {

            const sender =
                peer.getSenders()
                    .find(x =>
                        x.track &&
                        x.track.kind === "video"
                    );

            if (sender)
                await sender.replaceTrack(track);

        }

    }

    // ==========================================
    // Replace Audio Track
    // ==========================================

    async replaceAudioTrack(track) {

        for (const peer of this.peers.values()) {

            const sender =
                peer.getSenders()
                    .find(x =>
                        x.track &&
                        x.track.kind === "audio"
                    );

            if (sender)
                await sender.replaceTrack(track);

        }

    }

    // ==========================================
    // Restart ICE
    // ==========================================

    async restartIce(connectionId) {

        const pc =
            this.peers.get(connectionId);

        if (!pc)
            return;

        console.log(
            "[ICE] Restart:",
            connectionId
        );

        const offer =
            await pc.createOffer({

                iceRestart: true

            });

        await pc.setLocalDescription(offer);

        await this.connection.invoke(

            "SendOffer",

            connectionId,

            JSON.stringify(offer)

        );

    }
    // ==========================================
    // Remove Peer
    // ==========================================

    removePeer(connectionId) {

        const pc = this.peers.get(connectionId);

        if (!pc)
            return;

        console.log("[Peer] Removing:", connectionId);

        try {

            pc.ontrack = null;
            pc.onicecandidate = null;
            pc.onconnectionstatechange = null;
            pc.oniceconnectionstatechange = null;

            pc.close();

        }
        catch (err) {

            console.error(err);

        }

        this.peers.delete(connectionId);

        this.pendingCandidates.delete(connectionId);

        if (this.onPeerDisconnected)
            this.onPeerDisconnected(connectionId);

    }

    // ==========================================
    // Remove Everyone
    // ==========================================

    removeAll() {

        console.log(
            "[Peer] Closing all peers"
        );

        const ids =
            [...this.peers.keys()];

        ids.forEach(id => {

            this.removePeer(id);

        });

    }

    // ==========================================
    // Total Peers
    // ==========================================

    getCount() {

        return this.peers.size;

    }

    // ==========================================
    // Peer List
    // ==========================================

    getConnections() {

        return [...this.peers.keys()];

    }

    // ==========================================
    // Disconnect
    // ==========================================

    disconnect() {

        this.removeAll();

    }

    // ==========================================
    // Dispose
    // ==========================================

    dispose() {

        this.disconnect();

        this.connection = null;

        this.pendingCandidates.clear();

    }

}