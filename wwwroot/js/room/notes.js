// ==========================================
// Couples Connect
// Notes Manager
// ==========================================

export default class NotesManager {

    constructor(sessionId) {

        this.sessionId = sessionId;

        this.notesBox =
            document.getElementById("notesBox");

        this.saveState =
            document.getElementById("saveState");

        this.form =
            document.getElementById("notesForm");

        this.interval = null;

        this.lastSaved = "";

        this.saving = false;
        this.saveButton = document.getElementById("saveNotesButton");
        this.debounce = null;

    }

    // ==========================================
    // Initialize
    // ==========================================

    initialize() {

        if (!this.notesBox)
            return;

        this.lastSaved =
            this.notesBox.value;

        this.notesBox.addEventListener("input", () => {
            this.setStatus("Unsaved changes", "saving");
            clearTimeout(this.debounce);
            this.debounce = setTimeout(() => this.autoSave(), 1200);
        });

        this.interval = setInterval(() => this.autoSave(), 10000);

    }

    // ==========================================
    // Auto Save
    // ==========================================

    async autoSave() {

        if (this.saving)
            return;

        if (this.notesBox.value === this.lastSaved)
            return;

        await this.save(false);

    }

    appendTranscript({ text, speaker, timestamp }) {

        if (!this.notesBox || !text?.trim())
            return;

        const spokenAt = timestamp instanceof Date ? timestamp : new Date(timestamp);
        const time = Number.isNaN(spokenAt.getTime())
            ? new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
            : spokenAt.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
        const line = `[${time}] ${speaker || "Participant"}: ${text.trim()}`;
        const current = this.notesBox.value.trimEnd();

        this.notesBox.value = current ? `${current}\n${line}` : line;
        this.notesBox.scrollTop = this.notesBox.scrollHeight;
        this.setStatus("Transcription added — saving…", "saving");
        clearTimeout(this.debounce);
        this.debounce = setTimeout(() => this.autoSave(), 600);

    }

    // ==========================================
    // Save
    // ==========================================

    async save(showSuccess = true) {

        if (!this.notesBox)
            return;

        const notesBeingSaved = this.notesBox.value;
        let saveSucceeded = false;
        this.saving = true;
        this.saveButton?.setAttribute("disabled", "disabled");
        this.setStatus("Saving…", "saving");

        try {

            const token =
                this.form.querySelector(
                    'input[name="__RequestVerificationToken"]'
                ).value;

            const response =
                await fetch(
                    "?handler=SaveNotes",
                    {
                        method: "POST",

                        headers: {

                            "Content-Type":
                                "application/json",

                            "RequestVerificationToken":
                                token

                        },

                        body: JSON.stringify({

                            sessionId:
                                this.sessionId,

                            notes:
                                notesBeingSaved

                        })

                    }
                );

            if (!response.ok) throw new Error(`Save failed: ${response.status}`);
            const result = await response.json();

            if (result.success) {

                this.lastSaved =
                    notesBeingSaved;
                saveSucceeded = true;

                this.setStatus(`Saved ${new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`, "saved");

            }
            else {

                this.setStatus("Could not save — retry", "error");

            }

        }
        catch {
            this.setStatus("Could not save — retry", "error");

        }

        this.saving = false;
        this.saveButton?.removeAttribute("disabled");

        if (saveSucceeded && this.notesBox.value !== this.lastSaved) {
            clearTimeout(this.debounce);
            this.debounce = setTimeout(() => this.autoSave(), 0);
        }

    }

    // ==========================================
    // Status
    // ==========================================

    setStatus(text, state = "") {

        if (!this.saveState)
            return;

        this.saveState.innerText =
            text;
        this.saveState.className = `save-state ${state}`;

    }

    async flush() {

        if (!this.notesBox)
            return;

        clearTimeout(this.debounce);

        while (this.saving)
            await new Promise(resolve => setTimeout(resolve, 50));

        if (this.notesBox.value !== this.lastSaved)
            await this.save(false);

    }

    // ==========================================
    // Dispose
    // ==========================================

    dispose() {

        if (this.interval)

            clearInterval(
                this.interval
            );
        clearTimeout(this.debounce);

    }

}
