// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
<script>

    document.getElementById("resourceSearch")
    .addEventListener("keyup",function(){

        let value=this.value.toLowerCase();

    document.querySelectorAll(".resource-item")
.forEach(card=>{

const title=card.dataset.title.toLowerCase();

    card.style.display=
    title.includes(value)?"":"none";

});

});

    document.querySelectorAll(".filter-btn")
.forEach(btn=>{

        btn.onclick = function () {

            document.querySelectorAll(".filter-btn")
                .forEach(x => x.classList.remove("active"));

            this.classList.add("active");

            const category = this.dataset.category;

            document.querySelectorAll(".resource-item")
                .forEach(card => {

                    if (category === "all") {

                        card.style.display = "";

                    }
                    else {

                        card.style.display =
                            card.dataset.category === category ? "" : "none";

                    }

                });

        }

    });

</script>