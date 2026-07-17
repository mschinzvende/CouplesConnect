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

    }

    // ==========================================
    // Initialize
    // ==========================================

    initialize() {

        if (!this.notesBox)
            return;

        this.lastSaved =
            this.notesBox.value;

        this.interval =
            setInterval(() => {

                this.autoSave();

            }, 5000);

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

        this.setStatus("Saving...");

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

            const result =
                await response.json();

            if (result.success) {

                this.lastSaved =
                    this.notesBox.value;

                this.setStatus("Saved");

            }
            else {

                this.setStatus("Save failed");

            }

        }
        catch {

            this.setStatus("Connection error");

        }

        this.saving = false;

    }

    // ==========================================
    // Status
    // ==========================================

    setStatus(text) {

        if (!this.saveState)
            return;

        this.saveState.innerText =
            text;

    }

    // ==========================================
    // Dispose
    // ==========================================

    dispose() {

        if (this.interval)

            clearInterval(
                this.interval
            );

    }

}