//============================================================
// Couples Connect
// Date Night Generator
//============================================================

"use strict";

const TOTAL_STEPS = 5;

const STORAGE_KEY = "cc_date_generator";
const FAVORITES_KEY = "cc_date_favorites";

let currentStep = 1;

const app = {

    budget: "",

    mood: "",

    setting: "",

    time: ""

};

const tips = [

    "Great dates are built on conversation rather than money.",

    "Shared experiences create stronger memories than expensive gifts.",

    "Choose connection over perfection.",

    "Protect time together like an important appointment.",

    "The best date is the one you actually schedule."

];

const ideas = [

    {
        budget: "Low",
        mood: "Romantic",
        setting: "Indoors",
        time: "30 minutes",
        title: "Coffee & Conversation",
        description: "Prepare your favourite hot drink, switch off your phones and ask each other ten meaningful questions."
    },

    {
        budget: "Low",
        mood: "Fun",
        setting: "Indoors",
        time: "1 hour",
        title: "Cook Together",
        description: "Prepare dinner together and score each other's presentation."
    },

    {
        budget: "Low",
        mood: "Relaxed",
        setting: "Indoors",
        time: "1 hour",
        title: "Movie Night",
        description: "Choose an old favourite movie with homemade snacks."
    },

    {
        budget: "Low",
        mood: "Deep Talk",
        setting: "Either",
        time: "30 minutes",
        title: "Dream Session",
        description: "Talk about your dreams for the next five years."

    },

    {
        budget: "Medium",
        mood: "Romantic",
        setting: "Outdoors",
        time: "2+ hours",
        title: "Sunset Picnic",
        description: "Pack simple food and enjoy the sunset together."

    },

    {
        budget: "Medium",
        mood: "Fun",
        setting: "Outdoors",
        time: "1 hour",
        title: "Coffee Adventure",
        description: "Visit a café neither of you has ever tried."

    },

    {
        budget: "Medium",
        mood: "Adventure",
        setting: "Outdoors",
        time: "2+ hours",
        title: "Nature Walk",
        description: "Explore a botanical garden or hiking trail."

    },

    {
        budget: "Medium",
        mood: "Deep Talk",
        setting: "Indoors",
        time: "2+ hours",
        title: "Relationship Planning",
        description: "Plan your goals, finances and future together."

    },

    {
        budget: "High",
        mood: "Romantic",
        setting: "Either",
        time: "2+ hours",
        title: "Luxury Date",
        description: "Dress up and enjoy dinner somewhere memorable."

    },

    {
        budget: "High",
        mood: "Adventure",
        setting: "Outdoors",
        time: "2+ hours",
        title: "Weekend Escape",
        description: "Take a day trip or spend a night away together."

    }

];

//============================================================
// START
//============================================================

document.addEventListener("DOMContentLoaded", initialise);

function initialise() {

    registerEvents();

    loadState();

    loadFavorites();

    showStep(1);

}

//============================================================
// NAVIGATION
//============================================================

function showStep(step) {

    currentStep = step;

    document.querySelectorAll(".dn-card")
        .forEach(card => {

            card.classList.remove("active-step");

        });

    document.querySelectorAll(".dn-card")[step - 1]
        .classList.add("active-step");

    document.querySelectorAll(".dn-step")
        .forEach((stepItem, index) => {

            stepItem.classList.toggle(

                "active",

                index + 1 === step

            );

        });

    document.getElementById("btnPrevious").disabled =

        step === 1;

    document.getElementById("btnNext").innerHTML =

        step === TOTAL_STEPS

            ? 'Finish <i class="mdi mdi-check"></i>'

            : 'Next <i class="mdi mdi-arrow-right"></i>';

    document.getElementById("progressBar").style.width =

        ((step / TOTAL_STEPS) * 100) + "%";

    document.getElementById("dateTip").innerHTML =

        tips[step - 1];

}

function nextStep() {

    if (currentStep === TOTAL_STEPS) {

        finishGenerator();

        return;

    }

    showStep(currentStep + 1);

}

function previousStep() {

    if (currentStep === 1)
        return;

    showStep(currentStep - 1);

}

//============================================================
// EVENTS
//============================================================

function registerEvents() {

    document.getElementById("btnNext")
        .addEventListener("click", nextStep);

    document.getElementById("btnPrevious")
        .addEventListener("click", previousStep);

    document.querySelectorAll(".dn-step")
        .forEach((step, index) => {

            step.onclick = function () {

                showStep(index + 1);

            };

        });

    bindCards(".budget-card", "budget");

    bindCards(".mood-card", "mood");

    bindCards(".setting-card", "setting");

    bindCards(".time-card", "time");

    document.getElementById("btnGenerate")
        ?.addEventListener("click", generateDateIdea);

    document.getElementById("btnSaveFavourite")
        ?.addEventListener("click", saveFavourite);

    document.getElementById("btnDownload")
        ?.addEventListener("click", downloadFavorites);

    document.getElementById("notes")
        .addEventListener("input", autoSave);

}

//============================================================
// CARD SELECTION
//============================================================

function bindCards(selector, property) {

    document.querySelectorAll(selector)
        .forEach(card => {

            card.onclick = function () {

                document.querySelectorAll(selector)
                    .forEach(c => c.classList.remove("active"));

                this.classList.add("active");

                app[property] = this.dataset[property];

                autoSave();

            };

        });

}

//============================================================
// DATE GENERATION ENGINE
//============================================================

function scoreIdea(idea) {

    let score = 0;

    if (idea.budget === app.budget)
        score += 3;

    if (idea.mood === app.mood)
        score += 3;

    if (
        idea.setting === app.setting ||
        idea.setting === "Either" ||
        app.setting === "Either"
    )
        score += 2;

    if (
        idea.time === app.time
    )
        score += 2;

    return score;

}

function generateDateIdea() {

    let ranked = ideas.map(function (item) {

        return {

            idea: item,

            score: scoreIdea(item)

        };

    });

    ranked.sort(function (a, b) {

        return b.score - a.score;

    });

    let highest = ranked[0].score;

    let bestMatches = ranked.filter(function (item) {

        return item.score === highest;

    });

    let selected =

        bestMatches[

            Math.floor(

                Math.random() * bestMatches.length

            )

        ].idea;

    document.getElementById("ideaBox").innerHTML =

        `
        <h4>${selected.title}</h4>

        <p>${selected.description}</p>

        <div class="mt-3">

            <span class="badge bg-primary me-2">

                ${selected.budget}

            </span>

            <span class="badge bg-success me-2">

                ${selected.mood}

            </span>

            <span class="badge bg-info">

                ${selected.time}

            </span>

        </div>
        `;

    autoSave();

}

//============================================================
// AUTOSAVE
//============================================================

function autoSave() {

    const data = {

        budget: app.budget,

        mood: app.mood,

        setting: app.setting,

        time: app.time,

        notes: document.getElementById("notes").value,

        idea: document.getElementById("ideaBox").innerHTML

    };

    localStorage.setItem(

        STORAGE_KEY,

        JSON.stringify(data)

    );

    updateSaveStatus();

}

//============================================================
// LOAD STATE
//============================================================

function loadState() {

    let raw =

        localStorage.getItem(STORAGE_KEY);

    if (raw === null)
        return;

    let data =

        JSON.parse(raw);

    app.budget = data.budget || "";

    app.mood = data.mood || "";

    app.setting = data.setting || "";

    app.time = data.time || "";

    document.getElementById("notes").value =

        data.notes || "";

    if (data.idea) {

        document.getElementById("ideaBox").innerHTML =

            data.idea;

    }

    restoreSelection(

        ".budget-card",

        "budget",

        app.budget

    );

    restoreSelection(

        ".mood-card",

        "mood",

        app.mood

    );

    restoreSelection(

        ".setting-card",

        "setting",

        app.setting

    );

    restoreSelection(

        ".time-card",

        "time",

        app.time

    );

}

//============================================================
// RESTORE CARDS
//============================================================

function restoreSelection(

    selector,

    property,

    value

) {

    if (value === "")
        return;

    document.querySelectorAll(selector)

        .forEach(function (card) {

            if (

                card.dataset[property] === value

            ) {

                card.classList.add("active");

            }

        });

}

//============================================================
// SAVE STATUS
//============================================================

function updateSaveStatus() {

    const status =

        document.getElementById("saveStatus");

    status.innerHTML = "Saved";

    clearTimeout(window.saveTimer);

    window.saveTimer =

        setTimeout(function () {

            status.innerHTML = "Ready";

        }, 1500);

} 

//============================================================
// FAVORITES
//============================================================

function getFavorites() {

    return JSON.parse(

        localStorage.getItem(FAVORITES_KEY) || "[]"

    );

}

function saveFavourite() {

    const html =

        document.getElementById("ideaBox").innerHTML;

    const text =

        document.getElementById("ideaBox").innerText.trim();

    if (text === "") {

        alert("Please generate a date idea first.");

        return;

    }

    let favorites = getFavorites();

    favorites.unshift({

        created: new Date().toLocaleDateString(),

        budget: app.budget,

        mood: app.mood,

        setting: app.setting,

        time: app.time,

        notes: document.getElementById("notes").value,

        html: html

    });

    favorites = favorites.slice(0, 30);

    localStorage.setItem(

        FAVORITES_KEY,

        JSON.stringify(favorites)

    );

    loadFavorites();

}

function loadFavorites() {

    const favorites = getFavorites();

    const container =

        document.getElementById("favoritesContainer");

    const badge =

        document.getElementById("favoriteCount");

    badge.innerHTML =

        favorites.length + " Saved";

    if (favorites.length === 0) {

        container.innerHTML =

            `
        <div class="empty-favorites">

            <i class="mdi mdi-heart-outline"></i>

            <h5>No saved dates yet</h5>

            <p>Generate your first memorable date.</p>

        </div>
        `;

        return;

    }

    container.innerHTML = "";

    favorites.forEach(function (item, index) {

        container.innerHTML += `

        <div class="favorite-item">

            <div class="d-flex justify-content-between mb-3">

                <strong>

                    ${item.created}

                </strong>

                <button
                    class="btn btn-sm btn-outline-danger"
                    onclick="removeFavorite(${index})">

                    <i class="mdi mdi-delete"></i>

                </button>

            </div>

            ${item.html}

            <div class="favorite-notes">

                <strong>Notes</strong><br>

                ${item.notes || "None"}

            </div>

        </div>

        `;

    });

}

function removeFavorite(index) {

    let favorites = getFavorites();

    favorites.splice(index, 1);

    localStorage.setItem(

        FAVORITES_KEY,

        JSON.stringify(favorites)

    );

    loadFavorites();

}

//============================================================
// DOWNLOAD
//============================================================

function downloadFavorites() {

    const favorites = getFavorites();

    if (favorites.length === 0) {

        alert("There are no saved dates.");

        return;

    }

    let report = "";

    report += "COUPLES CONNECT\n";

    report += "DATE NIGHT COLLECTION\n\n";

    favorites.forEach(function (item, i) {

        report += "================================================\n";

        report += "DATE IDEA " + (i + 1) + "\n";

        report += "================================================\n\n";

        report += "Created : " + item.created + "\n";

        report += "Budget  : " + item.budget + "\n";

        report += "Mood    : " + item.mood + "\n";

        report += "Setting : " + item.setting + "\n";

        report += "Time    : " + item.time + "\n\n";

        report += item.notes + "\n\n";

    });

    const blob =

        new Blob(

            [report],

            {

                type: "text/plain"

            }

        );

    const url =

        URL.createObjectURL(blob);

    const a =

        document.createElement("a");

    a.href = url;

    a.download = "DateNightIdeas.txt";

    a.click();

    URL.revokeObjectURL(url);

}

//============================================================
// FINISH
//============================================================

function finishGenerator() {

    autoSave();

    document.querySelector(".dn-layout")

        .style.display = "none";

    document.querySelector(".dn-actionbar")

        .style.display = "none";

    document.getElementById("completionScreen")

        .style.display = "block";

}

//============================================================
// RESET
//============================================================

function resetGenerator() {

    if (!confirm("Reset this date planner?"))
        return;

    localStorage.removeItem(STORAGE_KEY);

    location.reload();

}

//============================================================
// AUTO SAVE
//============================================================

window.addEventListener(

    "beforeunload",

    autoSave

);

//============================================================
// KEYBOARD
//============================================================

document.addEventListener(

    "keydown",

    function (e) {

        if (e.key === "ArrowRight") {

            nextStep();

        }

        if (e.key === "ArrowLeft") {

            previousStep();

        }

        if (e.ctrlKey && e.key === "s") {

            e.preventDefault();

            autoSave();

        }

    }

);

//============================================================
// SWIPE SUPPORT
//============================================================

let swipeStart = 0;

document.addEventListener(

    "touchstart",

    function (e) {

        swipeStart =

            e.changedTouches[0].screenX;

    }

);

document.addEventListener(

    "touchend",

    function (e) {

        const swipeEnd =

            e.changedTouches[0].screenX;

        if (swipeStart - swipeEnd > 80) {

            nextStep();

        }

        if (swipeEnd - swipeStart > 80) {

            previousStep();

        }

    }

);

