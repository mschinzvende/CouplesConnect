//=========================================================
// Couples Connect
// Love Language Planner
//=========================================================

"use strict";

const TOTAL_STEPS = 5;

const STORAGE_KEY = "cc_love_language_planner";

let currentStep = 1;

const app = {

    language: "",

    actionCount: 5,

    intention: "",

    notes: "",

    plan: []

};

const tips = [

    "Love is communicated best in the language your partner naturally understands.",

    "Consistency beats intensity every time.",

    "Small daily actions build extraordinary relationships.",

    "Observe how your partner responds and adjust intentionally.",

    "Celebrate progress together at the end of every week."

];

const actions = {

    "Words of Affirmation": [

        "Send a sincere appreciation message.",

        "Say one specific thing your partner did well today.",

        "Leave a thoughtful handwritten note.",

        "Encourage them about a current challenge.",

        "Speak gratitude before going to bed.",

        "Praise their character rather than only achievements.",

        "Tell them why you admire them."

    ],

    "Quality Time": [

        "Spend 20 uninterrupted minutes together.",

        "Take a walk without using your phones.",

        "Share one meal together without screens.",

        "Create a simple home date.",

        "Ask meaningful questions and listen carefully.",

        "Plan the coming week together.",

        "Do one activity they genuinely enjoy."

    ],

    "Acts of Service": [

        "Complete one task before being asked.",

        "Take over a chore they dislike.",

        "Prepare something helpful in advance.",

        "Help organise something stressful.",

        "Offer practical support today.",

        "Run an errand for them.",

        "Ask how you can make today easier."

    ],

    "Receiving Gifts": [

        "Buy their favourite snack.",

        "Write a meaningful card.",

        "Surprise them with something thoughtful.",

        "Give a small symbolic gift.",

        "Create a memory box item.",

        "Bring home something they mentioned.",

        "Choose something that says 'I noticed you'."

    ],

    "Physical Touch": [

        "Start the day with a long hug.",

        "Hold hands during your walk.",

        "Sit close during conversation.",

        "Reconnect with affectionate touch after work.",

        "Be intentional about non-verbal affection.",

        "Offer comforting physical reassurance.",

        "Ask what affectionate gestures make them feel loved."

    ]

};

//=========================================================
// START
//=========================================================

document.addEventListener("DOMContentLoaded", initialise);

function initialise() {

    registerEvents();

    loadPlanner();

    showStep(1);

}

//=========================================================
// NAVIGATION
//=========================================================

function showStep(step) {

    currentStep = step;

    document.querySelectorAll(".ll-card")

        .forEach(card => {

            card.classList.remove("active-step");

        });

    document.querySelectorAll(".ll-card")[step - 1]

        .classList.add("active-step");

    document.querySelectorAll(".ll-step")

        .forEach((item, index) => {

            item.classList.toggle(

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

    document.getElementById("plannerTip").innerHTML =

        tips[step - 1];

    updateReview();

}

function nextStep() {

    if (currentStep === TOTAL_STEPS) {

        finishPlanner();

        return;

    }

    showStep(currentStep + 1);

}

function previousStep() {

    if (currentStep === 1)
        return;

    showStep(currentStep - 1);

}

//=========================================================
// EVENTS
//=========================================================

function registerEvents() {

    document.getElementById("btnNext").onclick = nextStep;

    document.getElementById("btnPrevious").onclick = previousStep;

    document.getElementById("btnSave").onclick = savePlanner;

    document.getElementById("btnDownload").onclick = downloadPlanner;

    document.getElementById("btnDownloadComplete").onclick = downloadPlanner;

    document.getElementById("btnGeneratePlan").onclick = generatePlan;

    document.querySelectorAll(".ll-step")

        .forEach((step, index) => {

            step.onclick = function () {

                showStep(index + 1);

            };

        });

    bindLanguageCards();

    bindInputs();

}

//=========================================================
// LOVE LANGUAGE
//=========================================================

function bindLanguageCards() {

    document.querySelectorAll(".language-card")

        .forEach(card => {

            card.onclick = function () {

                document.querySelectorAll(".language-card")

                    .forEach(c => c.classList.remove("active"));

                this.classList.add("active");

                app.language = this.dataset.language;

                autoSave();

                updateReview();

            };

        });

}

//=========================================================
// PLAN GENERATOR
//=========================================================

function generatePlan() {

    if (app.language === "") {

        alert("Please choose a love language first.");

        return;

    }

    app.actionCount =

        parseInt(

            document.getElementById("actionCount").value

        );

    app.plan =

        actions[app.language]

            .slice(0, app.actionCount)

            .map(function (item, index) {

                return {

                    day: index + 1,

                    text: item,

                    completed: false

                };

            });

    renderPlan();

    autoSave();

}
//=========================================================
// INPUTS
//=========================================================

function bindInputs() {

    document.getElementById("weeklyIntention")

        .addEventListener("input", function () {

            app.intention = this.value;

            autoSave();

            updateReview();

        });

    document.getElementById("reflectionNotes")

        .addEventListener("input", function () {

            app.notes = this.value;

            autoSave();

            updateReview();

        });

}

//=========================================================
// RENDER PLAN
//=========================================================

function renderPlan() {

    const container =

        document.getElementById("planContainer");

    if (app.plan.length === 0) {

        container.innerHTML =

            `<div class="text-center py-5">

            <i class="mdi mdi-calendar-star mdi-48px text-muted"></i>

            <h5 class="mt-3">

                No Weekly Plan Yet

            </h5>

            <p class="text-muted">

                Choose a love language and click Generate Weekly Plan.

            </p>

        </div>`;

        return;

    }

    container.innerHTML = "";

    app.plan.forEach(function (item, index) {

        container.innerHTML += `

        <div class="plan-item ${item.completed ? 'completed' : ''}">

            <div class="plan-check"

                 onclick="toggleTask(${index})">

                <i class="mdi ${item.completed ? 'mdi-check' : 'mdi-circle-outline'}"></i>

            </div>

            <div class="plan-content">

                <h5>

                    Action ${item.day}

                </h5>

                <p>

                    ${item.text}

                </p>

            </div>

            <div class="plan-status">

                ${item.completed ? 'Completed' : 'Pending'}

            </div>

        </div>

        `;

    });

}

//=========================================================
// TASK COMPLETE
//=========================================================

function toggleTask(index) {

    app.plan[index].completed =

        !app.plan[index].completed;

    renderPlan();

    autoSave();

}

//=========================================================
// AUTOSAVE
//=========================================================

function autoSave() {

    localStorage.setItem(

        STORAGE_KEY,

        JSON.stringify(app)

    );

    updateSaveStatus();

}

//=========================================================
// LOAD
//=========================================================

function loadPlanner() {

    const raw =

        localStorage.getItem(STORAGE_KEY);

    if (!raw)
        return;

    const data =

        JSON.parse(raw);

    app.language =

        data.language || "";

    app.actionCount =

        data.actionCount || 5;

    app.intention =

        data.intention || "";

    app.notes =

        data.notes || "";

    app.plan =

        data.plan || [];

    document.getElementById("actionCount").value =

        app.actionCount;

    document.getElementById("weeklyIntention").value =

        app.intention;

    document.getElementById("reflectionNotes").value =

        app.notes;

    if (app.language != "") {

        document.querySelectorAll(".language-card")

            .forEach(card => {

                if (card.dataset.language === app.language) {

                    card.classList.add("active");

                }

            });

    }

    renderPlan();

    updateReview();

}

//=========================================================
// REVIEW
//=========================================================

function updateReview() {

    document.getElementById("reviewLanguage").innerHTML =

        app.language || "-";

    document.getElementById("reviewIntention").innerHTML =

        app.intention || "-";

    document.getElementById("reviewReflection").innerHTML =

        app.notes || "-";

}

//=========================================================
// SAVE STATUS
//=========================================================

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

//=========================================================
// SAVE
//=========================================================

function savePlanner() {

    autoSave();

    alert("Love Language Planner saved successfully.");

}
//=========================================================
// DOWNLOAD
//=========================================================

function downloadPlanner() {

    const lines = [];

    lines.push("COUPLES CONNECT");
    lines.push("LOVE LANGUAGE PLANNER");
    lines.push("");

    lines.push("Love Language");
    lines.push(app.language || "");
    lines.push("");

    lines.push("Weekly Intention");
    lines.push(app.intention || "");
    lines.push("");

    lines.push("Weekly Action Plan");
    lines.push("");

    app.plan.forEach(function (item) {

        lines.push(

            "Action " + item.day +

            " : " + item.text +

            " [" + (item.completed ? "Completed" : "Pending") + "]"

        );

    });

    lines.push("");
    lines.push("Reflection Notes");
    lines.push(app.notes || "");

    const blob = new Blob(

        [lines.join("\n")],

        {

            type: "text/plain"

        }

    );

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    link.download = "LoveLanguagePlanner.txt";

    link.click();

    URL.revokeObjectURL(url);

}

//=========================================================
// FINISH
//=========================================================

function finishPlanner() {

    autoSave();

    updateReview();

    document.querySelector(".ll-layout")

        .style.display = "none";

    document.querySelector(".ll-actionbar")

        .style.display = "none";

    document.getElementById("completionScreen")

        .style.display = "block";

}

//=========================================================
// RESET
//=========================================================

function resetPlanner() {

    if (!confirm("Reset this Love Language Planner?"))
        return;

    localStorage.removeItem(STORAGE_KEY);

    app.language = "";
    app.actionCount = 5;
    app.intention = "";
    app.notes = "";
    app.plan = [];

    document.querySelectorAll(".language-card")

        .forEach(card => {

            card.classList.remove("active");

        });

    document.getElementById("actionCount").value = 5;

    document.getElementById("weeklyIntention").value = "";

    document.getElementById("reflectionNotes").value = "";

    renderPlan();

    updateReview();

    showStep(1);

}

//=========================================================
// EXPORT JSON
//=========================================================

function exportPlanner() {

    const blob = new Blob(

        [

            JSON.stringify(app, null, 4)

        ],

        {

            type: "application/json"

        }

    );

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    link.download = "LoveLanguagePlanner.json";

    link.click();

    URL.revokeObjectURL(url);

}

//=========================================================
// KEYBOARD SHORTCUTS
//=========================================================

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

        if (e.ctrlKey && e.key === "d") {

            e.preventDefault();

            downloadPlanner();

        }

    }

);

//=========================================================
// SWIPE SUPPORT
//=========================================================

let touchStartX = 0;

document.addEventListener(

    "touchstart",

    function (e) {

        touchStartX = e.changedTouches[0].screenX;

    }

);

document.addEventListener(

    "touchend",

    function (e) {

        const touchEndX = e.changedTouches[0].screenX;

        if (touchStartX - touchEndX > 80) {

            nextStep();

        }

        if (touchEndX - touchStartX > 80) {

            previousStep();

        }

    }

);

//=========================================================
// AUTO SAVE
//=========================================================

window.addEventListener(

    "beforeunload",

    autoSave

);

setInterval(function () {

    autoSave();

}, 30000);

//=========================================================
// AUTO REVIEW
//=========================================================

setInterval(function () {

    updateReview();

}, 1000);

