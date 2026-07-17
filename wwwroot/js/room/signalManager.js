// ==========================================
// Couples Connect
// Signal Manager
// ==========================================

export default class SignalManager {

    constructor(sessionId, isCounselor, peerManager) {

        this.sessionId = sessionId;

        this.isCounselor = isCounselor;

        this.peer = peerManager;

        this.connection = null;

        // -----------------------------
        // UI callbacks
        // -----------------------------

        this.onConnected = null;

        this.onDisconnected = null;

        this.onWaiting = null;

        this.onAdmitted = null;

        this.onUserJoined = null;

        this.onUserLeft = null;

        this.onUserWaiting = null;

        this.onRaiseHand = null;

    }

    // ==========================================
    // Connect
    // ==========================================

    async connect() {

        this.connection =
            new signalR.HubConnectionBuilder()

                .withUrl("/sessionHub")

                .withAutomaticReconnect()

                .build();

        this.peer.setConnection(this.connection);

        this.registerEvents();

        try {
            await this.connection.start();

            const pong = await this.connection.invoke("Ping");
            console.log("PING RESULT:", pong);
        }
        catch (err) {
            console.error(err);

            throw err;
        }

        console.log("[Signal] Connected");

        if (this.onConnected)
            this.onConnected();

        //------------------------------------
        // Counselor joins immediately
        //------------------------------------

        if (this.isCounselor) {

            await this.connection.invoke(

                "JoinSession",

                this.sessionId

            );

        }

        //------------------------------------
        // Client requests admission
        //------------------------------------

        else {

            if (this.onWaiting)
                this.onWaiting();

            await this.connection.invoke(

                "RequestJoin",

                this.sessionId

            );

        }

    }

    // ==========================================
    // Register Events
    // ==========================================

    registerEvents() {

        //------------------------------------
        // Existing Participants
        //------------------------------------

        this.connection.on(

            "ExistingParticipants",

            async users => {

                console.log(

                    "[Signal] Existing:",

                    users.length

                );

                for (const id of users) {

                    await this.peer.createPeer(id);

                    await this.peer.createOffer(id);

                }

            }

        );

        //------------------------------------
        // New User Joined
        //------------------------------------

        this.connection.on(

            "UserJoined",

            connectionId => {

                console.log(

                    "[Signal] Joined:",

                    connectionId

                );

                if (this.onUserJoined)

                    this.onUserJoined(connectionId);

            }

        );

        //------------------------------------
        // User Left
        //------------------------------------

        this.connection.on(

            "UserLeft",

            connectionId => {

                console.log(

                    "[Signal] Left:",

                    connectionId

                );

                this.peer.removePeer(

                    connectionId

                );

                if (this.onUserLeft)

                    this.onUserLeft(connectionId);

            }

        );

        //------------------------------------
        // Admitted
        //------------------------------------

        this.connection.on(
            "Admitted",
            async () => {

                console.log("[Signal] Admitted");

                await this.connection.invoke(
                    "JoinSession",
                    this.sessionId
                );

                if (this.onAdmitted)
                    await this.onAdmitted();

            }
        );

        //------------------------------------
        // Waiting User
        //------------------------------------

        this.connection.on(

            "UserWaiting",

            connectionId => {

                console.log(

                    "[Signal] Waiting:",

                    connectionId

                );

                if (this.onUserWaiting)

                    this.onUserWaiting(connectionId);

            }

        );

        //------------------------------------
        // Offer
        //------------------------------------

        this.connection.on(

            "ReceiveOffer",

            async (connectionId, offer) => {

                console.log(
                    "[Signal] Offer"
                );

                await this.peer.receiveOffer(

                    connectionId,

                    offer

                );

            }

        );

        //------------------------------------
        // Answer
        //------------------------------------

        this.connection.on(

            "ReceiveAnswer",

            async (connectionId, answer) => {

                console.log(
                    "[Signal] Answer"
                );

                await this.peer.receiveAnswer(

                    connectionId,

                    answer

                );

            }

        );

        //------------------------------------
        // ICE Candidate
        //------------------------------------

        this.connection.on(

            "ReceiveIceCandidate",

            async (connectionId, candidate) => {

                await this.peer.receiveIceCandidate(

                    connectionId,

                    candidate

                );

            }

        );

        //------------------------------------
        // Raised Hand
        //------------------------------------

        this.connection.on(

            "UserRaisedHand",

            connectionId => {

                console.log(
                    "[Signal] Raised Hand"
                );

                if (this.onRaiseHand)

                    this.onRaiseHand(connectionId);

            }

        );

        //------------------------------------
        // Force Unmute
        //------------------------------------

        this.connection.on("ForceUnmute", () => {

            if (window.forceUnmute)

                window.forceUnmute();

        });

        //------------------------------------
        // Force Unmute
        //------------------------------------

        this.connection.on(

            "ForceUnmute",

            () => {

                this.connection.on("ForceUnmute", () => {

                    if (window.forceUnmute)

                        window.forceUnmute();

                });

            }

        );

        //------------------------------------
        // Reconnecting
        //------------------------------------

        this.connection.onreconnecting(() => {

            console.log(
                "[Signal] Reconnecting..."
            );

            if (this.onDisconnected)

                this.onDisconnected();

        });

        //------------------------------------
        // Reconnected
        //------------------------------------

        this.connection.onreconnected(

            async () => {

                console.log(
                    "[Signal] Reconnected"
                );

                if (this.onConnected)

                    this.onConnected();

                if (this.isCounselor) {
                    await this.connection.invoke(
                        "JoinSession",
                        this.sessionId
                    );
                }
                else {
                    await this.connection.invoke(
                        "RequestJoin",
                        this.sessionId
                    );
                }

            }

        );

    }

    // ==========================================
    // Admit Waiting User
    // ==========================================

    async admit(connectionId) {

        await this.connection.invoke(

            "AdmitUser",

            this.sessionId,

            connectionId

        );

    }

    // ==========================================
    // Raise Hand
    // ==========================================

    async raiseHand() {

        await this.connection.invoke(

            "RaiseHand",

            this.sessionId

        );

    }

    // ==========================================
    // Force Mute
    // ==========================================

    async mute(connectionId) {

        await this.connection.invoke(

            "MuteUser",

            connectionId

        );

    }

    // ==========================================
    // Force Unmute
    // ==========================================

    async unmute(connectionId) {

        await this.connection.invoke(

            "UnmuteUser",

            connectionId

        );

    }

    // ==========================================
    // Disconnect
    // ==========================================

    async disconnect() {

        if (!this.connection)
            return;

        if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
            await this.connection.stop();
        }

    }

    // ==========================================
    // Dispose
    // ==========================================

    async dispose() {

        await this.disconnect();

        this.connection = null;

    }

}