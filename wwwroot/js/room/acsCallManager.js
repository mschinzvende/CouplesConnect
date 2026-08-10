import {
    CallClient,
    LocalVideoStream,
    VideoStreamRenderer
} from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";

export default class AcsCallManager {
    constructor({ token, groupId, onStateChanged }) {
        this.token = token;
        this.groupId = groupId;
        this.onStateChanged = onStateChanged;
        this.call = null;
        this.callAgent = null;
        this.localVideoStream = null;
        this.localRenderer = null;
        this.renderers = new Map();
        this.views = new Map();
        this.participants = new Map();
    }

    async join() {
        if (this.call) return;

        const callClient = new CallClient();
        const credential = new AzureCommunicationTokenCredential(this.token);
        this.callAgent = await callClient.createCallAgent(credential);

        const deviceManager = await callClient.getDeviceManager();
        await deviceManager.askDevicePermission({ audio: true, video: true });

        const cameras = await deviceManager.getCameras();
        const callOptions = { audioOptions: { muted: false } };

        if (cameras.length) {
            this.localVideoStream = new LocalVideoStream(cameras[0]);
            callOptions.videoOptions = { localVideoStreams: [this.localVideoStream] };
        }

        this.call = this.callAgent.join({ groupId: this.groupId }, callOptions);
        this.call.on("stateChanged", () => this.onStateChanged?.(this.call.state));
        this.call.on("remoteParticipantsUpdated", event => {
            event.added.forEach(participant => this.addRemoteParticipant(participant));
            event.removed.forEach(participant => this.removeRemoteParticipant(participant));
        });

        if (this.localVideoStream) await this.renderLocalVideo();
        this.call.remoteParticipants.forEach(participant => this.addRemoteParticipant(participant));
    }

    async renderLocalVideo() {
        this.localRenderer = new VideoStreamRenderer(this.localVideoStream);
        const view = await this.localRenderer.createView();
        this.addVideoCard("local", view, true);
    }

    addRemoteParticipant(participant) {
        const id = participant.identifier?.communicationUserId || participant.identifier?.rawId;
        if (!id || this.participants.has(id)) return;

        this.participants.set(id, participant);
        participant.on("videoStreamsUpdated", event => {
            event.added.forEach(stream => this.watchRemoteVideo(id, stream));
            event.removed.forEach(stream => this.removeVideoCard(`${id}:${stream.id}`));
        });

        participant.videoStreams.forEach(stream => this.watchRemoteVideo(id, stream));
    }

    removeRemoteParticipant(participant) {
        const id = participant.identifier?.communicationUserId || participant.identifier?.rawId;
        if (!id) return;
        this.participants.delete(id);
        [...this.views.keys()]
            .filter(key => key.startsWith(`${id}:`))
            .forEach(key => this.removeVideoCard(key));
    }

    watchRemoteVideo(participantId, stream) {
        const key = `${participantId}:${stream.id}`;
        const render = async () => {
            if (!stream.isAvailable || this.views.has(key)) return;
            const renderer = new VideoStreamRenderer(stream);
            const view = await renderer.createView();
            this.renderers.set(key, renderer);
            this.addVideoCard(key, view, false);
        };

        stream.on("isAvailableChanged", () => {
            if (stream.isAvailable) render().catch(console.error);
            else this.removeVideoCard(key);
        });
        render().catch(console.error);
    }

    addVideoCard(key, view, isLocal) {
        const grid = document.getElementById("videosContainer");
        const card = document.createElement("div");
        card.className = "participant-card";
        card.dataset.id = key;
        card.appendChild(view.target);

        const footer = document.createElement("div");
        footer.className = "participant-footer";
        footer.textContent = isLocal ? "You" : "Participant";
        card.appendChild(footer);
        grid.appendChild(card);

        this.views.set(key, { view, card });
        document.getElementById("participantCount").innerText = this.views.size;
    }

    removeVideoCard(key) {
        const item = this.views.get(key);
        if (!item) return;
        item.view.dispose();
        item.card.remove();
        this.views.delete(key);
        this.renderers.get(key)?.dispose();
        this.renderers.delete(key);
        document.getElementById("participantCount").innerText = this.views.size;
    }

    async toggleMute() {
        if (!this.call) return false;
        if (this.call.isMuted) await this.call.unmute();
        else await this.call.mute();
        return !this.call.isMuted;
    }

    async toggleVideo() {
        if (!this.call || !this.localVideoStream) return false;
        const enabled = this.call.localVideoStreams.includes(this.localVideoStream);
        if (enabled) await this.call.stopVideo(this.localVideoStream);
        else await this.call.startVideo(this.localVideoStream);
        return !enabled;
    }

    async leave() {
        if (this.call) await this.call.hangUp({ forEveryone: false });
        for (const key of [...this.views.keys()]) this.removeVideoCard(key);
        this.localRenderer?.dispose();
        this.callAgent?.dispose();
        this.call = null;
    }
}
