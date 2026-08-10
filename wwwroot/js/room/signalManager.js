export default class SignalManager {
    constructor(sessionId, isCounselor) {
        this.sessionId = sessionId;
        this.isCounselor = isCounselor;
        this.connection = null;
        this.onConnected = null;
        this.onDisconnected = null;
        this.onWaiting = null;
        this.onAdmitted = null;
        this.onUserWaiting = null;
    }

    async connect() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/sessionHub")
            .withAutomaticReconnect()
            .build();

        this.connection.on("Admitted", async () => {
            await this.connection.invoke("JoinSession", this.sessionId);
            await this.onAdmitted?.();
        });
        this.connection.on("UserWaiting", connectionId => this.onUserWaiting?.(connectionId));
        this.connection.onreconnecting(() => this.onDisconnected?.());
        this.connection.onreconnected(async () => {
            this.onConnected?.();
            if (this.isCounselor) await this.connection.invoke("JoinSession", this.sessionId);
            else await this.connection.invoke("RequestJoin", this.sessionId);
        });

        await this.connection.start();
        this.onConnected?.();
        if (this.isCounselor) await this.connection.invoke("JoinSession", this.sessionId);
        else {
            this.onWaiting?.();
            await this.connection.invoke("RequestJoin", this.sessionId);
        }
    }

    async admit(connectionId) {
        await this.connection.invoke("AdmitUser", this.sessionId, connectionId);
    }

    async raiseHand() {
        await this.connection.invoke("RaiseHand", this.sessionId.toString());
    }

    async dispose() {
        if (this.connection?.state !== signalR.HubConnectionState.Disconnected) {
            await this.connection.stop();
        }
    }
}
