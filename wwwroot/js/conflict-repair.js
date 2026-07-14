//====================================================
// Couples Connect - Conflict Repair
//====================================================

const TOTAL_STEPS = 6;

let currentStep = 1;

const reflectionTips = [

    "Describe only the facts. Leave assumptions and blame out of the story.",

    "Name emotions instead of behaviours. 'I felt hurt' is stronger than 'You ignored me.'",

    "Think about the need that wasn't met. Was it respect, affection, safety or understanding?",

    "Take ownership of your own contribution before looking at your partner's.",

    "A sincere apology acknowledges the impact of your actions without excuses.",

    "Choose one or two realistic actions you can both commit to."

];

const reflectionQuotes = [

    "Conflict doesn't destroy relationships. Unresolved conflict does.",

    "Understanding is more powerful than winning.",

    "Every emotion is trying to tell us something important.",

    "Ownership creates growth.",

    "Repair builds trust stronger than perfection.",

    "Healthy relationships grow through intentional effort."

];

document.addEventListener("DOMContentLoaded", function () {

    initialiseConflictRepair();

});

function initialiseConflictRepair() {

    initialiseSidebar();

    bindNavigation();

    bindStepClicks();

    loadAnswers();

    updateProgress();

    updateReflection();

    updateCharacterCounters();

    highlightCompleted();

    showStep(1);

}

//====================================================
// STEP NAVIGATION
//====================================================

function showStep(step) {

    currentStep = step;

    currentStep = step;

    const cards = document.querySelectorAll(".cr-card");

    cards.forEach(card => {

        card.classList.remove("active-step");

    });

    cards[step - 1].classList.add("active-step");

    document.querySelectorAll(".cr-step")
        .forEach((item, index) => {

            item.classList.toggle("active", index + 1 === step);

        });

    document.getElementById("currentStep").innerHTML = step;

    document.getElementById("btnPrevious").disabled =
        step === 1;

    document.getElementById("btnNext").innerHTML =
        step === TOTAL_STEPS
            ? 'Finish <i class="mdi mdi-check"></i>'
            : 'Next <i class="mdi mdi-arrow-right"></i>';

    updateProgress();

    updateReflection();

   }

function nextStep() {

    if (currentStep >= TOTAL_STEPS) {

        finishExercise();

        return;

    }

    showStep(currentStep + 1);

}

function previousStep() {

    if (currentStep <= 1)
        return;

    showStep(currentStep - 1);

}

//====================================================
// PROGRESS
//====================================================



function initialiseSidebar() {

    document.querySelectorAll(".cr-step")
        .forEach(function (step, index) {

            step.dataset.step = index + 1;

        });

}

function bindStepClicks() {

    document.querySelectorAll(".cr-step")
        .forEach(function (step) {

            step.addEventListener("click", function () {

                showStep(parseInt(this.dataset.step));

            });

        });

}

//====================================================
// REFLECTION PANEL
//====================================================

function updateReflection() {

    let tip = document.getElementById("reflectionTip");

    if (tip)
        tip.innerHTML = reflectionTips[currentStep - 1];

    let quote = document.getElementById("reflectionQuote");

    if (quote)
        quote.innerHTML = reflectionQuotes[currentStep - 1];

}

//====================================================
// BUTTONS
//====================================================

function bindNavigation() {

    document.getElementById("btnPrevious").disabled = true;

    document.getElementById("btnPrevious")
        .addEventListener("click", previousStep);

    document.getElementById("btnNext")
        .addEventListener("click", nextStep);

    document.addEventListener("keydown", function (e) {

        if (e.key === "ArrowRight")
            nextStep();

        if (e.key === "ArrowLeft")
            previousStep();

    });

}
//====================================================
// AUTOSAVE
//====================================================

const STORAGE_KEY = "cc_conflict_repair";

function bindAutosave() {

    document.querySelectorAll(".cr-card textarea")
        .forEach(function (textarea) {

            textarea.addEventListener("input", function () {

                saveAnswers();

                updateCharacterCounters();

                highlightCompleted();

                updateSaveIndicator();

            });

        });

}

function saveAnswers() {

    const data = {

        s1: document.getElementById("s1").value,
        s2: document.getElementById("s2").value,
        s3: document.getElementById("s3").value,
        s4: document.getElementById("s4").value,
        s5: document.getElementById("s5").value,
        s6: document.getElementById("s6").value,
        lastSaved: new Date().toISOString()

    };

    localStorage.setItem(

        STORAGE_KEY,

        JSON.stringify(data)

    );

}

function loadAnswers() {

    const raw = localStorage.getItem(STORAGE_KEY);

    if (!raw)
        return;

    const data = JSON.parse(raw);

    document.getElementById("s1").value = data.s1 ?? "";
    document.getElementById("s2").value = data.s2 ?? "";
    document.getElementById("s3").value = data.s3 ?? "";
    document.getElementById("s4").value = data.s4 ?? "";
    document.getElementById("s5").value = data.s5 ?? "";
    document.getElementById("s6").value = data.s6 ?? "";

}

//====================================================
// CHARACTER COUNTERS
//====================================================

function updateCharacterCounters() {

    document.querySelectorAll(".cr-card textarea")
        .forEach(function (textarea) {

            let counter =
                textarea.parentElement.querySelector(".cr-counter");

            if (!counter) {

                counter = document.createElement("div");

                counter.className = "cr-counter";

                textarea.parentElement.appendChild(counter);

            }

            counter.innerHTML =
                textarea.value.length + " characters";

        });

}

//====================================================
// STEP COMPLETION
//====================================================

function highlightCompleted() {

    document.querySelectorAll(".cr-card")
        .forEach(function (card, index) {

            const textarea =
                card.querySelector("textarea");

            const sidebar =
                document.querySelectorAll(".cr-step")[index];

            const number =
                sidebar.querySelector("span");

            if (textarea.value.trim().length > 10) {

                sidebar.classList.add("completed");

                number.innerHTML =

                    '<i class="mdi mdi-check"></i>';

                card.classList.add("completed");

            }

            else {

                sidebar.classList.remove("completed");

                number.innerHTML = index + 1;

                card.classList.remove("completed");

            }

        });

}

//====================================================
// SAVE INDICATOR
//====================================================

function updateSaveIndicator() {

    const status =
        document.getElementById("saveStatus");

    if (!status)
        return;

    status.className =
        "badge bg-success px-3 py-2";

    status.innerHTML =

        '<i class="mdi mdi-check-circle"></i> Saved';

    clearTimeout(window.saveTimer);

    window.saveTimer = setTimeout(function () {

        status.className =
            "badge bg-primary px-3 py-2";

        status.innerHTML =

            '<i class="mdi mdi-cloud-check"></i> Autosave';

    }, 1800);

}

//====================================================
// LIVE PROGRESS
//====================================================

function calculateCompletion() {

    let complete = 0;

    document.querySelectorAll(".cr-card textarea")
        .forEach(function (x) {

            if (x.value.trim().length > 10)

                complete++;

        });

    return complete;

}

function updateProgress() {

    const percent =
        (calculateCompletion() / TOTAL_STEPS) * 100;

    document.getElementById("progressBar")
        .style.width = percent + "%";

}

//====================================================
// INITIALISE
//====================================================


//====================================================
// DOWNLOAD
//====================================================

function downloadRepair() {

    const data = {

        "1. What happened?": document.getElementById("s1").value,
        "2. What did I feel?": document.getElementById("s2").value,
        "3. What did I need?": document.getElementById("s3").value,
        "4. What part do I own?": document.getElementById("s4").value,
        "5. Repair": document.getElementById("s5").value,
        "6. Moving Forward": document.getElementById("s6").value

    };

    let report = "";

    report += "=============================================\n";
    report += "       COUPLES CONNECT\n";
    report += "       CONFLICT REPAIR REPORT\n";
    report += "=============================================\n\n";

    report += "Generated : " + new Date().toLocaleString() + "\n\n";

    Object.keys(data).forEach(function (key) {

        report += key + "\n";
        report += "---------------------------------------------\n";
        report += data[key] + "\n\n";

    });

    const blob = new Blob([report], {

        type: "text/plain"

    });

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");

    link.href = url;

    link.download = "ConflictRepair.txt";

    link.click();

    URL.revokeObjectURL(url);

}

//====================================================
// RESET
//====================================================

function resetRepair() {

    if (!confirm("Are you sure you want to clear all answers?"))
        return;

    document.querySelectorAll(".cr-card textarea")
        .forEach(function (x) {

            x.value = "";

        });

    localStorage.removeItem(STORAGE_KEY);

    updateCharacterCounters();

    highlightCompleted();

    showStep(1);

    updateSaveIndicator();

}

//====================================================
// FINISH
//====================================================

function finishExercise() {

    saveAnswers();

    if (calculateCompletion() < TOTAL_STEPS) {

        if (!confirm(
            "Some questions are still incomplete.\n\nFinish anyway?"
        ))
            return;

    }

    showCompletionScreen();

}

//====================================================
// COMPLETION SCREEN
//====================================================

function showCompletionScreen() {

    document.querySelector(".cr-layout")
        .style.display = "none";

    document.querySelector(".cr-actionbar")
        .style.display = "none";

    let complete =
        document.getElementById("completionScreen");

    if (!complete)
        return;

    complete.style.display = "block";

    complete.classList.add("fade-enter");

}

//====================================================
// PRINT
//====================================================

function printExercise() {

    window.print();

}

//====================================================
// EXPORT JSON
//====================================================

function exportJson() {

    const json =
        JSON.stringify(getAnswers(), null, 4);

    const blob =
        new Blob([json], {

            type: "application/json"

        });

    const link =
        document.createElement("a");

    link.href =
        URL.createObjectURL(blob);

    link.download =
        "ConflictRepair.json";

    link.click();

}

//====================================================
// ANSWERS
//====================================================

function getAnswers() {

    return {

        s1: document.getElementById("s1").value,
        s2: document.getElementById("s2").value,
        s3: document.getElementById("s3").value,
        s4: document.getElementById("s4").value,
        s5: document.getElementById("s5").value,
        s6: document.getElementById("s6").value

    };

}

//====================================================
// KEYBOARD SHORTCUTS
//====================================================

document.addEventListener("keydown", function (e) {

    if (e.ctrlKey && e.key === "s") {

        e.preventDefault();

        saveAnswers();

        updateSaveIndicator();

    }

    if (e.ctrlKey && e.key === "p") {

        e.preventDefault();

        printExercise();

    }

});

//====================================================
// SWIPE NAVIGATION
//====================================================

let touchStartX = 0;

document.addEventListener("touchstart", function (e) {

    touchStartX = e.changedTouches[0].screenX;

});

document.addEventListener("touchend", function (e) {

    let touchEndX = e.changedTouches[0].screenX;

    if (touchEndX - touchStartX > 80) {

        previousStep();

    }

    if (touchStartX - touchEndX > 80) {

        nextStep();

    }

});

//====================================================
// BEFORE LEAVE
//====================================================

window.addEventListener("beforeunload", function () {

    saveAnswers();

});

//====================================================
// START
//====================================================

