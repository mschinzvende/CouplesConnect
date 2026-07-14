//=====================================================
// Couples Connect - Family & Boundaries
//=====================================================

const TOTAL_STEPS = 5;

let currentStep = 1;

let selectedArea = "";

const STORAGE_KEY = "cc_family_boundaries";

const tips = [

    "Healthy boundaries protect relationships—they don't damage them.",

    "Discuss together before responding to others.",

    "Boundaries should be clear, respectful and consistent.",

    "Kindness and firmness can exist together.",

    "Review your boundaries regularly as life changes."

];

document.addEventListener("DOMContentLoaded", function () {

    initialise();

});

//=====================================================
// INITIALISE
//=====================================================

function initialise() {

    bindAreaCards();

    bindNavigation();

    bindInputs();

    loadPlan();

    updateProgress();

    updateTip();

    showStep(1);

}

//=====================================================
// STEP NAVIGATION
//=====================================================

function showStep(step) {

    currentStep = step;

    const cards = document.querySelectorAll(".fb-card");

    cards.forEach(function (card) {

        card.classList.remove("active-step");

    });

    cards[step - 1].classList.add("active-step");

    document.querySelectorAll(".fb-step")
        .forEach(function (item, index) {

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

    updateProgress();

    updateTip();

    updateReview();

}

function nextStep() {

    if (currentStep === TOTAL_STEPS) {

        finishPlan();

        return;

    }

    showStep(currentStep + 1);

}

function previousStep() {

    if (currentStep === 1)
        return;

    showStep(currentStep - 1);

}

//=====================================================
// SIDEBAR
//=====================================================

function updateProgress() {

    document.getElementById("progressBar").style.width =

        ((currentStep / TOTAL_STEPS) * 100) + "%";

}

function updateTip() {

    document.getElementById("boundaryTip").innerHTML =

        tips[currentStep - 1];

}

//=====================================================
// AREA SELECTION
//=====================================================

function bindAreaCards() {

    document.querySelectorAll(".area-card")
        .forEach(function (card) {

            card.addEventListener("click", function () {

                document.querySelectorAll(".area-card")
                    .forEach(function (x) {

                        x.classList.remove("active");

                    });

                this.classList.add("active");

                selectedArea = this.dataset.area;

                autoSave();

                updateReview();

            });

        });

}

//=====================================================
// INPUT EVENTS
//=====================================================

function bindInputs() {

    document.querySelectorAll("textarea")
        .forEach(function (box) {

            box.addEventListener("input", function () {

                autoSave();

                updateReview();

            });

        });

}

//=====================================================
// AUTOSAVE
//=====================================================

function autoSave() {

    const data = {

        area: selectedArea,

        challenge: document.getElementById("challenge").value,

        boundary: document.getElementById("boundary").value,

        script: document.getElementById("script").value,

        lastSaved: new Date().toISOString()

    };

    localStorage.setItem(

        STORAGE_KEY,

        JSON.stringify(data)

    );

    updateSaveStatus();

}

//=====================================================
// LOAD
//=====================================================

function loadPlan() {

    const raw = localStorage.getItem(STORAGE_KEY);

    if (!raw)
        return;

    const data = JSON.parse(raw);

    selectedArea = data.area ?? "";

    document.getElementById("challenge").value =
        data.challenge ?? "";

    document.getElementById("boundary").value =
        data.boundary ?? "";

    document.getElementById("script").value =
        data.script ?? "";

    if (selectedArea) {

        document.querySelectorAll(".area-card")
            .forEach(function (card) {

                if (card.dataset.area === selectedArea)

                    card.classList.add("active");

            });

    }

    updateReview();

}

//=====================================================
// REVIEW
//=====================================================

function updateReview() {

    document.getElementById("reviewArea").innerHTML =
        selectedArea || "-";

    document.getElementById("reviewChallenge").innerHTML =
        document.getElementById("challenge").value || "-";

    document.getElementById("reviewBoundary").innerHTML =
        document.getElementById("boundary").value || "-";

    document.getElementById("reviewScript").innerHTML =
        document.getElementById("script").value || "-";

}

//=====================================================
// SAVE STATUS
//=====================================================

function updateSaveStatus() {

    const status =
        document.getElementById("saveStatus");

    status.innerHTML = "Saved";

    clearTimeout(window.fbSaveTimer);

    window.fbSaveTimer = setTimeout(function () {

        status.innerHTML = "Ready";

    }, 1500);

}

//=====================================================
// SAVE PLAN
//=====================================================

function savePlan() {

    autoSave();

    alert("Boundary plan saved successfully.");

}

//=====================================================
// DOWNLOAD
//=====================================================

function downloadPlan() {

    const report =

        `COUPLES CONNECT
FAMILY & BOUNDARIES PLAN

=======================================

AREA

${selectedArea}

=======================================

CURRENT CHALLENGE

${document.getElementById("challenge").value}

=======================================

OUR BOUNDARY

${document.getElementById("boundary").value}

=======================================

COMMUNICATION SCRIPT

${document.getElementById("script").value}

`;

    const blob = new Blob(

        [report],

        {

            type: "text/plain"

        }

    );

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    link.download = "Family-Boundary-Plan.txt";

    link.click();

    URL.revokeObjectURL(url);

}
//=====================================================
// FINISH
//=====================================================

function finishPlan() {

    autoSave();

    updateReview();

    document.querySelector(".fb-layout")
        .style.display = "none";

    document.querySelector(".fb-actionbar")
        .style.display = "none";

    document.getElementById("completionScreen")
        .style.display = "block";

}

//=====================================================
// BUTTONS
//=====================================================

function bindNavigation() {

    document.getElementById("btnPrevious")
        .addEventListener("click", previousStep);

    document.getElementById("btnNext")
        .addEventListener("click", nextStep);

}

//=====================================================
// STEP CLICK NAVIGATION
//=====================================================

document.querySelectorAll(".fb-step")
    .forEach(function (step, index) {

        step.addEventListener("click", function () {

            showStep(index + 1);

        });

    });

//=====================================================
// KEYBOARD SHORTCUTS
//=====================================================

document.addEventListener("keydown", function (e) {

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

    if (e.ctrlKey && e.key === "p") {

        e.preventDefault();

        downloadPlan();

    }

});

//=====================================================
// TOUCH SUPPORT
//=====================================================

let touchStartX = 0;

document.addEventListener("touchstart", function (e) {

    touchStartX = e.changedTouches[0].screenX;

});

document.addEventListener("touchend", function (e) {

    const touchEndX =
        e.changedTouches[0].screenX;

    if (touchStartX - touchEndX > 80) {

        nextStep();

    }

    if (touchEndX - touchStartX > 80) {

        previousStep();

    }

});

//=====================================================
// AUTO REVIEW UPDATE
//=====================================================

setInterval(function () {

    updateReview();

}, 1000);

//=====================================================
// AUTO SAVE EVERY 30 SECONDS
//=====================================================

setInterval(function () {

    autoSave();

}, 30000);

//=====================================================
// BEFORE LEAVE
//=====================================================

window.addEventListener("beforeunload", function () {

    autoSave();

});

//=====================================================
// RESET PLAN
//=====================================================

function resetPlan() {

    if (!confirm("Clear the current boundary plan?"))
        return;

    selectedArea = "";

    document.querySelectorAll(".area-card")
        .forEach(function (card) {

            card.classList.remove("active");

        });

    document.getElementById("challenge").value = "";

    document.getElementById("boundary").value = "";

    document.getElementById("script").value = "";

    localStorage.removeItem(STORAGE_KEY);

    updateReview();

    showStep(1);

    document.getElementById("saveStatus").innerHTML =
        "Ready";

}

//=====================================================
// EXPORT JSON
//=====================================================

function exportPlanJson() {

    const data = {

        area: selectedArea,

        challenge: document.getElementById("challenge").value,

        boundary: document.getElementById("boundary").value,

        script: document.getElementById("script").value,

        exported: new Date().toISOString()

    };

    const blob = new Blob(

        [JSON.stringify(data, null, 4)],

        {

            type: "application/json"

        }

    );

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    link.download = "FamilyBoundaryPlan.json";

    link.click();

    URL.revokeObjectURL(url);

}