//=====================================================
// Couples Connect - Daily Check In
//=====================================================

const TOTAL_STEPS = 5;

let currentStep = 1;

let selectedMood = "";

let selectedEnergy = "";

const STORAGE_KEY = "cc_daily_checkin";

const tips = [

    "Start with curiosity instead of assumptions.",

    "Tell your partner what you need instead of expecting them to guess.",

    "Specific appreciation builds stronger relationships.",

    "Shared goals strengthen teamwork.",

    "Small daily rituals create lasting connection."

];

const rituals = [

    "Sit together for five minutes without phones and ask: 'What would make today lighter for you?'",

    "Share one thing you appreciate about each other before leaving the house.",

    "Hold hands for one minute without speaking.",

    "Take a ten-minute walk together after dinner.",

    "Pray together before bed.",

    "Ask your partner: 'How can I support you today?'",

    "Share one success from today and celebrate it together.",

    "End the day with a six-second hug."

];

document.addEventListener("DOMContentLoaded", function () {

    initialise();

});

function initialise() {

    bindMoodCards();

    bindEnergyButtons();

    bindFocusCards();

    bindNavigation();

    bindTextAreas();

    loadEntry();

    newRitual(false);

    updateProgress();

    updateTip();

    updateStreak();

    showStep(1);

}

//=====================================================
// STEP NAVIGATION
//=====================================================

function showStep(step) {

    currentStep = step;

    const cards =
        document.querySelectorAll(".dc-card");

    cards.forEach(function (card) {

        card.classList.remove("active-step");

    });

    cards[step - 1].classList.add("active-step");

    document.querySelectorAll(".dc-step")
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

}

function nextStep() {

    if (currentStep === TOTAL_STEPS) {

        finishCheckIn();

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

    const percent =
        (currentStep / TOTAL_STEPS) * 100;

    document.getElementById("progressBar").style.width =
        percent + "%";

}

function updateTip() {

    document.getElementById("dailyTip").innerHTML =
        tips[currentStep - 1];

}

//=====================================================
// MOOD
//=====================================================

function bindMoodCards() {

    document.querySelectorAll(".mood-card")
        .forEach(function (card) {

            card.addEventListener("click", function () {

                document.querySelectorAll(".mood-card")
                    .forEach(function (c) {

                        c.classList.remove("active");

                    });

                this.classList.add("active");

                selectedMood =
                    this.dataset.value;

                autoSave();

            });

        });

}

//=====================================================
// ENERGY
//=====================================================

function bindEnergyButtons() {

    document.querySelectorAll(".energy-btn")
        .forEach(function (button) {

            button.addEventListener("click", function () {

                document.querySelectorAll(".energy-btn")
                    .forEach(function (b) {

                        b.classList.remove("active");

                    });

                this.classList.add("active");

                selectedEnergy =
                    this.dataset.energy;

                autoSave();

            });

        });

}
//=====================================================
// FOCUS CARDS
//=====================================================

function bindFocusCards() {

    document.querySelectorAll(".focus-card")
        .forEach(function (card) {

            card.addEventListener("click", function () {

                document.querySelectorAll(".focus-card")
                    .forEach(function (c) {

                        c.classList.remove("active");

                    });

                this.classList.add("active");

                document.getElementById("focus").value =
                    this.innerText.trim();

                autoSave();

            });

        });

}

//=====================================================
// TEXTAREA EVENTS
//=====================================================

function bindTextAreas() {

    document.querySelectorAll("textarea,input")
        .forEach(function (x) {

            x.addEventListener("input", function () {

                autoSave();

            });

        });

}

//=====================================================
// SAVE
//=====================================================

function autoSave() {

    const data = {

        mood: selectedMood,

        energy: selectedEnergy,

        need: document.getElementById("need").value,

        appreciation: document.getElementById("appreciation").value,

        focus: document.getElementById("focus").value,

        ritual: document.getElementById("ritualText").innerText,

        lastSaved: new Date().toISOString()

    };

    localStorage.setItem(

        STORAGE_KEY,

        JSON.stringify(data)

    );

    updateSaveStatus();

    updateCompletion();

}

function loadEntry() {

    const raw =
        localStorage.getItem(STORAGE_KEY);

    if (!raw)
        return;

    const data = JSON.parse(raw);

    selectedMood =
        data.mood ?? "";

    selectedEnergy =
        data.energy ?? "";

    document.getElementById("need").value =
        data.need ?? "";

    document.getElementById("appreciation").value =
        data.appreciation ?? "";

    document.getElementById("focus").value =
        data.focus ?? "";

    if (data.ritual)
        document.getElementById("ritualText").innerHTML =
            data.ritual;

    if (selectedMood) {

        document.querySelectorAll(".mood-card")
            .forEach(function (card) {

                if (card.dataset.value === selectedMood)

                    card.classList.add("active");

            });

    }

    if (selectedEnergy) {

        document.querySelectorAll(".energy-btn")
            .forEach(function (btn) {

                if (btn.dataset.energy === selectedEnergy)

                    btn.classList.add("active");

            });

    }

    if (data.focus) {

        document.querySelectorAll(".focus-card")
            .forEach(function (card) {

                if (card.innerText.trim() === data.focus)

                    card.classList.add("active");

            });

    }

}

//=====================================================
// SAVE STATUS
//=====================================================

function updateSaveStatus() {

    const save =
        document.getElementById("saveStatus");

    save.innerHTML = "Saved";

    clearTimeout(window.saveTimer);

    window.saveTimer = setTimeout(function () {

        save.innerHTML = "Autosaved";

    }, 1500);

}

//=====================================================
// COMPLETION
//=====================================================

function updateCompletion() {

    let completed = 0;

    if (selectedMood)
        completed++;

    if (selectedEnergy)
        completed++;

    if (document.getElementById("need").value.trim())
        completed++;

    if (document.getElementById("appreciation").value.trim())
        completed++;

    if (document.getElementById("focus").value.trim())
        completed++;

    const percentage =
        (completed / 5) * 100;

    document.getElementById("progressBar").style.width =
        percentage + "%";

}

//=====================================================
// STREAK
//=====================================================

function todayKey() {

    return new Date().toISOString().substring(0, 10);

}

function updateStreak() {

    let streak =
        Number(localStorage.getItem("cc_daily_streak") || 0);

    let last =
        localStorage.getItem("cc_daily_last");

    document.getElementById("streakDays").innerHTML =
        streak + (streak === 1 ? " Day" : " Days");

}

function incrementStreak() {

    let today =
        todayKey();

    let last =
        localStorage.getItem("cc_daily_last");

    let streak =
        Number(localStorage.getItem("cc_daily_streak") || 0);

    if (last !== today) {

        streak++;

        localStorage.setItem(

            "cc_daily_streak",

            streak

        );

        localStorage.setItem(

            "cc_daily_last",

            today

        );

    }

    updateStreak();

}

//=====================================================
// RITUAL
//=====================================================

function newRitual(save = true) {

    const ritual =
        rituals[Math.floor(Math.random() * rituals.length)];

    document.getElementById("ritualText").innerHTML =
        ritual;

    if (save)
        autoSave();

}
//=====================================================
// DOWNLOAD
//=====================================================

function downloadCheckIn() {

    const report =
        `COUPLES CONNECT
DAILY CHECK-IN

Date: ${new Date().toLocaleDateString()}

----------------------------------------

Mood

${selectedMood}

----------------------------------------

Energy

${selectedEnergy}

----------------------------------------

What I need today

${document.getElementById("need").value}

----------------------------------------

Appreciation

${document.getElementById("appreciation").value}

----------------------------------------

Shared Focus

${document.getElementById("focus").value}

----------------------------------------

Today's Ritual

${document.getElementById("ritualText").innerText}

`;

    const blob = new Blob(

        [report],

        { type: "text/plain" }

    );

    const url = URL.createObjectURL(blob);

    const a = document.createElement("a");

    a.href = url;

    a.download =

        "Daily-Check-In.txt";

    a.click();

    URL.revokeObjectURL(url);

}

//=====================================================
// RESET
//=====================================================

function resetCheckIn() {

    if (!confirm("Clear today's check-in?"))
        return;

    selectedMood = "";

    selectedEnergy = "";

    document.querySelectorAll(".mood-card")
        .forEach(x => x.classList.remove("active"));

    document.querySelectorAll(".energy-btn")
        .forEach(x => x.classList.remove("active"));

    document.querySelectorAll(".focus-card")
        .forEach(x => x.classList.remove("active"));

    document.getElementById("need").value = "";

    document.getElementById("appreciation").value = "";

    document.getElementById("focus").value = "";

    newRitual(false);

    localStorage.removeItem(STORAGE_KEY);

    updateCompletion();

    showStep(1);

}

//=====================================================
// FINISH
//=====================================================

function finishCheckIn() {

    autoSave();

    incrementStreak();

    document.querySelector(".dc-layout")
        .style.display = "none";

    document.querySelector(".dc-actionbar")
        .style.display = "none";

    document.getElementById("completionScreen")
        .style.display = "block";

    window.scrollTo({

        top: 0,

        behavior: "smooth"

    });

}

//=====================================================
// BUTTON EVENTS
//=====================================================

function bindNavigation() {

    document.getElementById("btnPrevious")
        .addEventListener("click", previousStep);

    document.getElementById("btnNext")
        .addEventListener("click", nextStep);

}

//=====================================================
// KEYBOARD
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

});

//=====================================================
// BEFORE LEAVE
//=====================================================

window.addEventListener("beforeunload", function () {

    autoSave();

});

//=====================================================
// TOUCH SUPPORT
//=====================================================

let touchStart = 0;

document.addEventListener("touchstart", function (e) {

    touchStart = e.changedTouches[0].screenX;

});

document.addEventListener("touchend", function (e) {

    let touchEnd = e.changedTouches[0].screenX;

    if (touchStart - touchEnd > 80)

        nextStep();

    if (touchEnd - touchStart > 80)

        previousStep();

});

//=====================================================
// STEP CLICK NAVIGATION
//=====================================================

document.querySelectorAll(".dc-step")
    .forEach(function (step, index) {

        step.addEventListener("click", function () {

            showStep(index + 1);

        });

    });

//=====================================================
// INITIALISE
//=====================================================

document.addEventListener("DOMContentLoaded", function () {

    initialise();

});