//=========================================================
// Couples Connect
// Guided Intimacy Builder
//=========================================================

"use strict";

const TOTAL_STEPS = 5;

const STORAGE_KEY = "cc_guided_intimacy";

let currentStep = 1;

const app = {

    focus: "",

    selectedActivity: "",

    weeklyPlan: "",

    commitment: ""

};

const tips = [

    "Emotional intimacy grows through consistent conversations.",

    "Small daily acts of affection build lasting trust.",

    "Protect intentional time together every week.",

    "Choose vulnerability over defensiveness.",

    "Celebrate progress instead of perfection."

];

const activityMap = {

    "Emotional closeness": [

        "Take 10 minutes to ask: 'How are you really feeling this week?'",

        "Share one pressure you are carrying and one thing that would help.",

        "End the day with one appreciation and one gentle check-in."

    ],

    "Affection and warmth": [

        "Hold hands during a walk or while praying together.",

        "Give a meaningful hug before work and before bed.",

        "Leave a thoughtful note or encouraging message."

    ],

    "Quality time": [

        "Schedule a 20-minute phone-free conversation.",

        "Cook together without distractions.",

        "Protect one weekly ritual that belongs only to the two of you."

    ],

    "Vulnerability and honesty": [

        "Share one fear, one hope and one need.",

        "Talk about what helps you feel loved.",

        "Practice listening without interrupting."

    ],

    "Repair after distance": [

        "Discuss what has made you feel disconnected recently.",

        "Apologize sincerely without blaming.",

        "Choose one small habit that helps you reconnect."

    ]

};

//=========================================================
// START
//=========================================================

document.addEventListener("DOMContentLoaded", initialise);

function initialise() {

    registerEvents();

    loadPlan();

    showStep(1);

}

//=========================================================
// NAVIGATION
//=========================================================

function showStep(step) {

    currentStep = step;

    document.querySelectorAll(".gi-card")
        .forEach(card => {

            card.classList.remove("active-step");

        });

    document.querySelectorAll(".gi-card")[step - 1]

        .classList.add("active-step");

    document.querySelectorAll(".gi-step")

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

    document.getElementById("connectionTip").innerHTML =

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

    document.getElementById("btnNext")
        .onclick = nextStep;

    document.getElementById("btnPrevious")
        .onclick = previousStep;

    document.getElementById("btnSave")
        .onclick = savePlan;

    document.getElementById("btnDownload")
        .onclick = downloadPlan;

    document.getElementById("btnDownloadComplete")
        .onclick = downloadPlan;

    document.querySelectorAll(".gi-step")
        .forEach((step, index) => {

            step.onclick = function () {

                showStep(index + 1);

            };

        });

    bindFocusCards();

    bindInputs();

}

//=========================================================
// FOCUS CARDS
//=========================================================

function bindFocusCards() {

    document.querySelectorAll(".focus-card")

        .forEach(card => {

            card.onclick = function () {

                document.querySelectorAll(".focus-card")

                    .forEach(c => c.classList.remove("active"));

                this.classList.add("active");

                app.focus = this.dataset.focus;

                loadActivities();

                autoSave();

            };

        });

}

//=========================================================
// ACTIVITIES
//=========================================================

function loadActivities() {

    const container =

        document.getElementById("activityContainer");

    container.innerHTML = "";

    if (!activityMap[app.focus])
        return;

    activityMap[app.focus]

        .forEach(function (item) {

            container.innerHTML += `

        <div class="activity-card">

            <i class="mdi mdi-heart-outline"></i>

            <p>${item}</p>

        </div>

        `;

        });

}
//=========================================================
// ACTIVITY SELECTION
//=========================================================

function bindActivityCards() {

    document.querySelectorAll(".activity-card")

        .forEach(card => {

            card.onclick = function () {

                document.querySelectorAll(".activity-card")

                    .forEach(c => c.classList.remove("active"));

                this.classList.add("active");

                app.selectedActivity =

                    this.querySelector("p").innerText;

                autoSave();

                updateReview();

            };

        });

}

//=========================================================
// INPUTS
//=========================================================

function bindInputs() {

    document.getElementById("weeklyPlan")

        .addEventListener("input", function () {

            app.weeklyPlan = this.value;

            autoSave();

            updateReview();

        });

    document.getElementById("commitment")

        .addEventListener("input", function () {

            app.commitment = this.value;

            autoSave();

            updateReview();

        });

}

//=========================================================
// AUTOSAVE
//=========================================================

function autoSave() {

    const data = {

        focus: app.focus,

        activity: app.selectedActivity,

        weeklyPlan: app.weeklyPlan,

        commitment: app.commitment

    };

    localStorage.setItem(

        STORAGE_KEY,

        JSON.stringify(data)

    );

    updateSaveStatus();

}

//=========================================================
// LOAD PLAN
//=========================================================

function loadPlan() {

    const raw =

        localStorage.getItem(STORAGE_KEY);

    if (!raw)
        return;

    const data =

        JSON.parse(raw);

    app.focus = data.focus || "";

    app.selectedActivity = data.activity || "";

    app.weeklyPlan = data.weeklyPlan || "";

    app.commitment = data.commitment || "";

    document.getElementById("weeklyPlan").value =

        app.weeklyPlan;

    document.getElementById("commitment").value =

        app.commitment;

    if (app.focus != "") {

        document.querySelectorAll(".focus-card")

            .forEach(card => {

                if (card.dataset.focus === app.focus) {

                    card.classList.add("active");

                }

            });

        loadActivities();

    }

    updateReview();

}

//=========================================================
// REVIEW
//=========================================================

function updateReview() {

    document.getElementById("reviewFocus").innerHTML =

        app.focus || "-";

    document.getElementById("reviewPlan").innerHTML =

        app.weeklyPlan || "-";

    document.getElementById("reviewCommitment").innerHTML =

        app.commitment || "-";

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

function savePlan() {

    autoSave();

    alert("Connection plan saved successfully.");

}

//=========================================================
// DOWNLOAD
//=========================================================

function downloadPlan() {

    const report =

        `COUPLES CONNECT
GUIDED INTIMACY BUILDER

======================================

FOCUS AREA

${app.focus}

======================================

SELECTED ACTIVITY

${app.selectedActivity}

======================================

WEEKLY CONNECTION PLAN

${app.weeklyPlan}

======================================

OUR COMMITMENT

${app.commitment}

`;

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

    a.download = "Guided-Intimacy-Plan.txt";

    a.click();

    URL.revokeObjectURL(url);

}

//=========================================================
// FINISH
//=========================================================

function finishPlanner() {

    autoSave();

    updateReview();

    document.querySelector(".gi-layout")
        .style.display = "none";

    document.querySelector(".gi-actionbar")
        .style.display = "none";

    document.getElementById("completionScreen")
        .style.display = "block";

}

//=========================================================
// RESET
//=========================================================

function resetPlanner() {

    if (!confirm("Reset your connection plan?"))
        return;

    localStorage.removeItem(STORAGE_KEY);

    app.focus = "";
    app.selectedActivity = "";
    app.weeklyPlan = "";
    app.commitment = "";

    document.querySelectorAll(".focus-card")
        .forEach(card => card.classList.remove("active"));

    document.getElementById("activityContainer").innerHTML = "";

    document.getElementById("weeklyPlan").value = "";

    document.getElementById("commitment").value = "";

    updateReview();

    showStep(1);

}

//=========================================================
// ACTIVITY CARD REBIND
//=========================================================

const originalLoadActivities = loadActivities;

loadActivities = function () {

    originalLoadActivities();

    bindActivityCards();

    if (app.selectedActivity === "")
        return;

    document.querySelectorAll(".activity-card")
        .forEach(card => {

            if (card.querySelector("p").innerText === app.selectedActivity) {

                card.classList.add("active");

            }

        });

};

//=========================================================
// AUTO SAVE
//=========================================================

window.addEventListener(

    "beforeunload",

    autoSave

);

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

            downloadPlan();

        }

    }

);

//=========================================================
// MOBILE SWIPE SUPPORT
//=========================================================

let touchStartX = 0;

document.addEventListener(

    "touchstart",

    function (e) {

        touchStartX =

            e.changedTouches[0].screenX;

    }

);

document.addEventListener(

    "touchend",

    function (e) {

        const touchEndX =

            e.changedTouches[0].screenX;

        if (touchStartX - touchEndX > 80) {

            nextStep();

        }

        if (touchEndX - touchStartX > 80) {

            previousStep();

        }

    }

);

//=========================================================
// AUTO REVIEW REFRESH
//=========================================================

setInterval(function () {

    updateReview();

}, 1000);

//=========================================================
// PERIODIC AUTOSAVE
//=========================================================

setInterval(function () {

    autoSave();

}, 30000);

//=========================================================
// EXPORT JSON
//=========================================================

function exportJson() {

    const data = {

        focus: app.focus,

        activity: app.selectedActivity,

        weeklyPlan: app.weeklyPlan,

        commitment: app.commitment,

        exported: new Date().toISOString()

    };

    const blob = new Blob(

        [

            JSON.stringify(data, null, 4)

        ],

        {

            type: "application/json"

        }

    );

    const url =

        URL.createObjectURL(blob);

    const link =

        document.createElement("a");

    link.href = url;

    link.download = "GuidedIntimacyPlan.json";

    link.click();

    URL.revokeObjectURL(url);

}