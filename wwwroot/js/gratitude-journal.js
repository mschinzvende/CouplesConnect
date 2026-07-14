"use strict";

class Journal {

    constructor() {

        this.memories = [];

        this.currentId = 0;

        this.modal = null;

        this.deleteModal = null;

    }

    init() {

        this.modal =
            new bootstrap.Modal("#memoryModal");

        this.deleteModal =
            new bootstrap.Modal("#deleteModal");

        this.bindEvents();

        this.loadMemories();

    }

    bindEvents() {

        $("#btnAddMemory,#btnHeroMemory,#btnTimelineMemory,#btnEmptyMemory")
            .on("click", () => {

                this.newMemory();

            });

        $("#btnSaveMemory")
            .on("click", () => {

                this.save();

            });

        $("#txtSearch")
            .on("keyup", () => {

                this.filter();

            });

        $("#ddlCategory,#ddlMood,#ddlSort")
            .on("change", () => {

                this.filter();

            });

        $(".mood-btn")
            .on("click", function () {

                $(".mood-btn").removeClass("active");

                $(this).addClass("active");

                $("#EntryMood")
                    .val($(this).data("mood"));

            });

        $("#btnDeleteMemory")
            .on("click", () => {

                this.delete();

            });

    }
    loadMemories() {

        this.memories = [];

        // Sample data

        this.memories.push({

            id: 1,

            title: "Our First Date",

            category: "Date Night",

            mood: "😍",

            memory: "We had an amazing dinner together and talked for hours.",

            gratitude: "Grateful for finding each other.",

            date: "2026-07-10",

            favourite: true,

            tags: ["love", "date"],

            photos: [
                "https://images.unsplash.com/photo-1516589091380-5d8e87df6999?auto=format&fit=crop&w=800&q=80"
            ]

        });

        this.render();

    }
    render() {

        const timeline =
            $("#timelineContainer");

        timeline.empty();

        if (this.memories.length === 0) {

            $("#emptyState")
                .removeClass("d-none");

            return;

        }

        $("#emptyState")
            .addClass("d-none");

        this.memories.forEach(memory => {

            timeline.append(

                this.createCard(memory)

            );

        });

        this.updateDashboard();

    }
    createCard(memory) {

        const template =
            document
                .getElementById("memoryCardTemplate");

        const clone =
            template.content.cloneNode(true);

        $(clone)
            .find(".title")
            .text(memory.title);

        $(clone)
            .find(".memoryText")
            .text(memory.memory);

        $(clone)
            .find(".gratitudeText")
            .text(memory.gratitude);

        $(clone)
            .find(".category")
            .text(memory.category);

        $(clone)
            .find(".mood")
            .text(memory.mood);

        $(clone)
            .find(".day")
            .text(moment(memory.date).format("DD"));

        $(clone)
            .find(".month")
            .text(moment(memory.date).format("MMM"));

        $(clone)
            .find(".full-date")
            .text(moment(memory.date).format("DD MMMM YYYY"));

        if (memory.favourite)

            $(clone)
                .find(".favourite")
                .removeClass("d-none");

        this.renderTags(clone, memory);

        this.renderPhotos(clone, memory);

        this.bindCardEvents(clone, memory);

        return clone;

    }

    renderTags(card, memory) {

        const container =
            $(card).find(".tags");

        container.empty();

        if (!memory.tags)
            return;

        memory.tags.forEach(tag => {

            container.append(

                `<span class="badge bg-indigo-lt">

                    #${tag}

                 </span>`

            );

        });

    }

    renderPhotos(card, memory) {

        const gallery =
            $(card).find(".gallery");

        gallery.empty();

        if (!memory.photos)
            return;

        memory.photos.forEach(photo => {

            gallery.append(

                `<div class="photo-item">

                    <img class="photo"

                         src="${photo}" />

                 </div>`

            );

        });

    }
    save() {

        const memory = {

            id: $("#MemoryId").val(),

            date: $("#EntryDate").val(),

            title: $("#EntryTitle").val(),

            category: $("#EntryCategory").val(),

            mood: $("#EntryMood").val(),

            memory: $("#EntryMemory").val(),

            gratitude: $("#EntryGratitude").val(),

            tags: $("#EntryTags").val(),

            favourite: $("#EntryFavourite").is(":checked")

        };

        const formData = new FormData();

        formData.append("memory", JSON.stringify(memory));

        const files = $("#EntryPhotos")[0].files;

        for (let i = 0; i < files.length; i++) {

            formData.append("photos", files[i]);

        }

        const handler = memory.id == ""
            ? "SaveMemory"
            : "UpdateMemory";

        $("#pageLoader").removeClass("d-none");

        $.ajax({

            url: "?handler=" + handler,

            type: "POST",

            processData: false,

            contentType: false,

            headers: {

                "RequestVerificationToken":
                    $('input[name="__RequestVerificationToken"]').val()

            },

            data: formData,

            success: () => {

                this.modal.hide();

                this.showToast(
                    "Memory saved successfully.",
                    "success");

                this.loadMemories();

            },

            error: () => {

                this.showToast(
                    "Unable to save memory.",
                    "danger");

            },

            complete: () => {

                $("#pageLoader").addClass("d-none");

            }

        });

    }

    edit(memory) {

        $("#MemoryId").val(memory.id);

        $("#EntryDate").val(memory.date);

        $("#EntryTitle").val(memory.title);

        $("#EntryCategory").val(memory.category);

        $("#EntryMemory").val(memory.memory);

        $("#EntryGratitude").val(memory.gratitude);

        $("#EntryTags").val(memory.tags.join(","));

        $("#EntryFavourite").prop(
            "checked",
            memory.favourite);

        $(".mood-btn").removeClass("active");

        $(`.mood-btn[data-mood="${memory.mood}"]`)
            .addClass("active");

        $("#EntryMood").val(memory.mood);

        this.modal.show();

    }

    newMemory() {

        $("#MemoryId").val("");

        $("#EntryDate").val(moment().format("YYYY-MM-DD"));

        $("#EntryTitle").val("");

        $("#EntryMemory").val("");

        $("#EntryGratitude").val("");

        $("#EntryTags").val("");

        $("#EntryFavourite").prop("checked", false);

        $("#EntryPhotos").val("");

        $(".mood-btn").removeClass("active");

        $(".mood-btn:first").addClass("active");

        $("#EntryMood").val("😍");

        this.modal.show();

    }
    delete() {

        $.ajax({

            url: "?handler=DeleteMemory",

            type: "POST",

            headers: {

                "RequestVerificationToken":
                    $('input[name="__RequestVerificationToken"]').val()

            },

            data: {

                id: this.currentId

            },

            success: () => {

                this.deleteModal.hide();

                this.showToast(
                    "Memory deleted.",
                    "warning");

                this.loadMemories();

            }

        });

    }
    toggleFavourite(id) {

        $.ajax({

            url: "?handler=ToggleFavourite",

            type: "POST",

            headers: {

                "RequestVerificationToken":
                    $('input[name="__RequestVerificationToken"]').val()

            },

            data: {

                id: id

            },

            success: () => {

                this.loadMemories();

            }

        });

    }

    bindCardEvents(card, memory) {

        $(card).find(".btnEdit")
            .on("click", () => {

                this.edit(memory);

            });

        $(card).find(".btnDelete")
            .on("click", () => {

                this.currentId = memory.id;

                this.deleteModal.show();

            });

        $(card).find(".btnFavourite")
            .on("click", () => {

                this.toggleFavourite(memory.id);

            });

        $(card).find(".btnShare")
            .on("click", () => {

                navigator.share({

                    title: memory.title,

                    text: memory.memory

                });

            });

        $(card).find(".btnDownload")
            .on("click", () => {

                window.open(

                    "?handler=Download&id=" +
                    memory.id,

                    "_blank"

                );

            });

    }

    updateDashboard() {

        $("#lblMemories")
            .text(this.memories.length);

        $("#lblFavourite")
            .text(

                this.memories.filter(x => x.favourite).length

            );

        $("#lblGratitude")
            .text(

                this.memories.filter(x =>

                    x.gratitude !== null &&
                    x.gratitude !== "")

                    .length

            );

        $("#lblStreak")
            .text(

                this.calculateStreak()

            );

    }

    calculateStreak() {

        if (this.memories.length === 0)
            return 0;

        const dates = [...new Set(

            this.memories
                .map(x => moment(x.date).format("YYYY-MM-DD"))

        )];

        dates.sort().reverse();

        let streak = 1;

        for (let i = 1; i < dates.length; i++) {

            const diff = moment(dates[i - 1])
                .diff(moment(dates[i]), "days");

            if (diff === 1)
                streak++;
            else
                break;

        }

        return streak;

    }

    filter() {

        const search = $("#txtSearch").val().toLowerCase();

        const category = $("#ddlCategory").val();

        const mood = $("#ddlMood").val();

        let filtered = [...this.memories];

        if (search !== "") {

            filtered = filtered.filter(x =>

                x.title.toLowerCase().includes(search) ||

                x.memory.toLowerCase().includes(search) ||

                x.gratitude.toLowerCase().includes(search)

            );

        }

        if (category !== "") {

            filtered = filtered.filter(x =>

                x.category === category);

        }

        if (mood !== "") {

            filtered = filtered.filter(x =>

                x.mood === mood);

        }

        switch ($("#ddlSort").val()) {

            case "old":

                filtered.sort((a, b) =>
                    new Date(a.date) - new Date(b.date));

                break;

            case "fav":

                filtered =
                    filtered.filter(x => x.favourite);

                break;

            default:

                filtered.sort((a, b) =>
                    new Date(b.date) - new Date(a.date));

                break;

        }

        this.renderFiltered(filtered);

    }

    renderFiltered(memories) {

        const timeline =
            $("#timelineContainer");

        timeline.empty();

        if (memories.length === 0) {

            $("#emptyState")
                .removeClass("d-none");

            return;

        }

        $("#emptyState")
            .addClass("d-none");

        memories.forEach(memory => {

            timeline.append(

                this.createCard(memory)

            );

        });

    }

    exportJournal() {

        window.open(

            "?handler=Export",

            "_blank"

        );

    }

    showToast(message, type) {

        const toast = `

<div class="toast show">

<div class="toast-header">

<strong class="me-auto">

Couples Connect

</strong>

<button class="btn-close"

data-bs-dismiss="toast">

</button>

</div>

<div class="toast-body bg-${type}-lt">

${message}

</div>

</div>`;

        $(".toast-container")

            .append(toast);

        setTimeout(function () {

            $(".toast")

                .first()

                .remove();

        }, 4000);

    }

}
$(function () {

    window.journal = new Journal();

    journal.init();

});

