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

    // ==========================================
    // Save
    // ==========================================

    async save(showSuccess = true) {

        if (!this.notesBox)
            return;

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
                                this.notesBox.value

                        })

                    }
                );

            if (!response.ok) throw new Error(`Save failed: ${response.status}`);
            const result = await response.json();

            if (result.success) {

                this.lastSaved =
                    this.notesBox.value;

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
