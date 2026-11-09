import {
    CallClient,
    LocalVideoStream,
    VideoStreamRenderer
} from "@azure/communication-calling";
import { Features } from "@azure/communication-calling";
import { AzureCommunicationTokenCredential } from "@azure/communication-common";

export default class AcsCallManager {
    constructor({ token, groupId, displayName, onStateChanged, onRaisedHandsChanged, onVideoStateChanged, onMuteStateChanged, onScreenSharingChanged, onCaptionReceived, onCaptionsStateChanged }) {
        this.token = token;
        this.groupId = groupId;
        this.displayName = displayName;
        this.onStateChanged = onStateChanged;
        this.onRaisedHandsChanged = onRaisedHandsChanged;
        this.onVideoStateChanged = onVideoStateChanged;
        this.onMuteStateChanged = onMuteStateChanged;
        this.onScreenSharingChanged = onScreenSharingChanged;
        this.onCaptionReceived = onCaptionReceived;
        this.onCaptionsStateChanged = onCaptionsStateChanged;
        this.call = null;
        this.callAgent = null;
        this.callClient = null;
        this.deviceManager = null;
        this.camera = null;
        this.localVideoStream = null;
        this.localRenderer = null;
        this.renderers = new Map();
        this.views = new Map();
        this.participants = new Map();
        this.featuredKey = null;
        this.handRaised = false;
        this.captions = null;
        this.captionHandler = null;
        this.captionsActiveHandler = null;
    }

    async join() {
        if (this.call) return;

        this.callClient = new CallClient();
        const credential = new AzureCommunicationTokenCredential(this.token);
        this.callAgent = await this.callClient.createCallAgent(credential, { displayName: this.displayName });

        this.deviceManager = await this.callClient.getDeviceManager();
        await this.deviceManager.askDevicePermission({ audio: true, video: true });

        const cameras = await this.deviceManager.getCameras();
        const callOptions = { audioOptions: { muted: false } };

        if (cameras.length) {
            this.camera = cameras[0];
            this.localVideoStream = new LocalVideoStream(this.camera);
            callOptions.videoOptions = { localVideoStreams: [this.localVideoStream] };
        }

        this.call = this.callAgent.join({ groupId: this.groupId }, callOptions);
        this.call.on("stateChanged", () => this.onStateChanged?.(this.call.state));
        this.call.on("remoteParticipantsUpdated", event => {
            event.added.forEach(participant => this.addRemoteParticipant(participant));
            event.removed.forEach(participant => this.removeRemoteParticipant(participant));
        });
        this.call.on("isScreenSharingOnChanged", () => this.onScreenSharingChanged?.(this.call.isScreenSharingOn));
        this.call.on("isLocalVideoStartedChanged", () => this.onVideoStateChanged?.(this.call.isLocalVideoStarted));
        this.call.on("isMutedChanged", () => this.onMuteStateChanged?.(!this.call.isMuted));
        this.raiseHandFeature = this.call.feature(Features.RaiseHand);
        this.raiseHandFeature.on("raisedHandEvent", () => this.notifyRaisedHandsChanged());
        this.raiseHandFeature.on("loweredHandEvent", () => this.notifyRaisedHandsChanged());

        const captionsFeature = this.call.feature(Features.Captions);
        this.captions = captionsFeature.captions;
        this.captionHandler = caption => this.onCaptionReceived?.(caption);
        this.captionsActiveHandler = () => this.onCaptionsStateChanged?.(this.captions.isCaptionsFeatureActive);
        this.captions.on("CaptionsReceived", this.captionHandler);
        this.captions.on("CaptionsActiveChanged", this.captionsActiveHandler);
        this.onStateChanged?.(this.call.state);

        if (this.localVideoStream) await this.renderLocalVideo();
        this.call.remoteParticipants.forEach(participant => this.addRemoteParticipant(participant));
    }

    async renderLocalVideo() {
        if (this.views.has("local")) this.removeVideoCard("local");
        this.localRenderer?.dispose();
        this.localRenderer = new VideoStreamRenderer(this.localVideoStream);
        const view = await this.createFitView(this.localRenderer);
        this.addVideoCard("local", view, true);
    }

    async createFitView(renderer) {
        const view = await renderer.createView({ scalingMode: "Fit" });
        // Explicitly update as well: some mobile browsers retain the renderer's
        // previous default until the view has been attached to the page.
        await view.updateScalingMode?.("Fit");
        this.enforceContainedVideo(view.target);
        return view;
    }

    enforceContainedVideo(target) {
        target.classList.add("acs-video-target");
        const apply = () => target.querySelectorAll("video").forEach(video => {
            video.style.setProperty("width", "100%", "important");
            video.style.setProperty("height", "100%", "important");
            video.style.setProperty("max-width", "100%", "important");
            video.style.setProperty("max-height", "100%", "important");
            video.style.setProperty("object-fit", "contain", "important");
            video.style.setProperty("object-position", "center", "important");
        });
        apply();
        requestAnimationFrame(apply);
        setTimeout(apply, 250);
    }

    addRemoteParticipant(participant) {
        const id = participant.identifier?.communicationUserId || participant.identifier?.rawId;
        if (!id || this.participants.has(id)) return;

        this.participants.set(id, participant);
        this.updateParticipantCount();
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
        this.updateParticipantCount();
        [...this.views.keys()]
            .filter(key => key.startsWith(`${id}:`))
            .forEach(key => this.removeVideoCard(key));
    }

    watchRemoteVideo(participantId, stream) {
        const key = `${participantId}:${stream.id}`;
        const render = async () => {
            if (!stream.isAvailable || this.views.has(key)) return;
            const renderer = new VideoStreamRenderer(stream);
            // ACS defaults to Crop, which cuts off portrait phone cameras in a
            // landscape counselor view. Fit preserves the complete frame.
            const view = await this.createFitView(renderer);
            this.renderers.set(key, renderer);
            if (!this.featuredKey) this.setFeaturedVideo(key, view);
            else this.addVideoCard(key, view, false);
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
        card.tabIndex = 0;
        card.setAttribute("role", "button");
        card.setAttribute("aria-label", "Show this video in the main view");
        const promote = () => this.promoteToFeatured(key);
        card.addEventListener("click", promote);
        card.addEventListener("keydown", event => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                promote();
            }
        });
        card.appendChild(view.target);

        const footer = document.createElement("div");
        footer.className = "participant-footer";
        footer.textContent = isLocal ? "You" : "Participant";
        card.appendChild(footer);
        grid.appendChild(card);

        this.views.set(key, { view, card });
        this.updateParticipantCount();
    }

    promoteToFeatured(key) {
        if (key === this.featuredKey) return;
        const next = this.views.get(key);
        if (!next?.card) return;

        const previousKey = this.featuredKey;
        const previous = previousKey ? this.views.get(previousKey) : null;
        next.card.remove();
        this.setFeaturedVideo(key, next.view);

        if (previous?.view) {
            this.addVideoCard(previousKey, previous.view, previousKey === "local");
        }
    }

    setFeaturedVideo(key, view) {
        const stage = document.getElementById("mainStage");
        document.getElementById("videoEmptyState")?.remove();
        stage.appendChild(view.target);
        this.views.set(key, { view, card: null });
        this.featuredKey = key;
        this.updateParticipantCount();
    }

    removeVideoCard(key) {
        const item = this.views.get(key);
        if (!item) return;
        item.view.dispose();
        item.card?.remove();
        this.views.delete(key);
        this.renderers.get(key)?.dispose();
        this.renderers.delete(key);
        if (this.featuredKey === key) {
            this.featuredKey = null;
            const empty = document.createElement("div");
            empty.id = "videoEmptyState";
            empty.className = "video-empty-state";
            empty.innerHTML = "<span class='video-empty-icon'>◉</span><strong>Waiting for participant video</strong><small>Your video appears in the tile below.</small>";
            document.getElementById("mainStage").appendChild(empty);
        }
        this.updateParticipantCount();
    }

    async toggleMute() {
        if (!this.call) return false;
        if (this.call.isMuted) await this.call.unmute();
        else await this.call.mute();
        return !this.call.isMuted;
    }

    async toggleVideo() {
        if (!this.call) return false;
        const enabled = this.call.isLocalVideoStarted;
        if (enabled) {
            const activeStream = this.call.localVideoStreams.find(stream => stream.mediaStreamType === "Video") ?? this.localVideoStream;
            if (activeStream) await this.call.stopVideo(activeStream);
        } else {
            if (!this.camera) {
                const cameras = await this.deviceManager.getCameras();
                this.camera = cameras[0];
            }
            if (!this.camera) return false;
            this.localVideoStream = new LocalVideoStream(this.camera);
            await this.call.startVideo(this.localVideoStream);
            await this.renderLocalVideo();
        }
        return !enabled;
    }

    async toggleScreenShare() {
        if (!this.call) return false;
        const wasSharing = this.call.isScreenSharingOn;
        if (wasSharing) await this.call.stopScreenSharing();
        else await this.call.startScreenSharing();
        return !wasSharing;
    }

    async toggleRaiseHand() {
        if (!this.call) return false;
        if (this.handRaised) await this.raiseHandFeature.lowerHand();
        else await this.raiseHandFeature.raiseHand();
        this.handRaised = !this.handRaised;
        this.notifyRaisedHandsChanged();
        return this.handRaised;
    }

    notifyRaisedHandsChanged() {
        this.onRaisedHandsChanged?.(this.raiseHandFeature.getRaisedHands());
    }

    async startTranscription(spokenLanguage = "en-US") {
        if (!this.captions) throw new Error("Join the call before starting transcription.");
        const supportedLanguage = this.captions.supportedSpokenLanguages
            .find(language => language.toLowerCase() === spokenLanguage.toLowerCase());
        if (!supportedLanguage) {
            throw new Error(`${spokenLanguage} is not supported for live transcription.`);
        }
        await this.captions.startCaptions({ spokenLanguage: supportedLanguage });
    }

    async stopTranscription() {
        if (!this.captions?.isCaptionsFeatureActive) return;
        await this.captions.stopCaptions();
    }

    updateParticipantCount() {
        const counter = document.getElementById("participantCount");
        if (counter) counter.innerText = String(this.participants.size + 1);
    }

    async leave() {
        if (this.captions) {
            if (this.captionHandler) this.captions.off("CaptionsReceived", this.captionHandler);
            if (this.captionsActiveHandler) this.captions.off("CaptionsActiveChanged", this.captionsActiveHandler);
        }
        if (this.call) await this.call.hangUp({ forEveryone: false });
        for (const key of [...this.views.keys()]) this.removeVideoCard(key);
        this.localRenderer?.dispose();
        this.callAgent?.dispose();
        this.call = null;
    }
}
