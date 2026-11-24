(function () {
    document.addEventListener("DOMContentLoaded", function () {

        // ---- Phone Number: block any non-digit character as the user types ----
        var phoneInput = document.getElementById("PhoneNumber");
        if (phoneInput) {
            phoneInput.addEventListener("input", function () {
                var digitsOnly = phoneInput.value.replace(/[^0-9]/g, "");
                if (digitsOnly !== phoneInput.value) {
                    phoneInput.value = digitsOnly;
                }
            });
            // Also block the keystroke itself for browsers that fire keypress for non-digit keys
            phoneInput.addEventListener("keypress", function (e) {
                var char = String.fromCharCode(e.which || e.keyCode);
                if (!/[0-9]/.test(char)) {
                    e.preventDefault();
                }
            });
            // Digits typed via IME/paste land in "input"; strip non-digits from pasted text too
            phoneInput.addEventListener("paste", function (e) {
                var pasted = (e.clipboardData || window.clipboardData).getData("text");
                if (/[^0-9]/.test(pasted)) {
                    e.preventDefault();
                    var digitsOnly = pasted.replace(/[^0-9]/g, "");
                    var start = phoneInput.selectionStart;
                    var end = phoneInput.selectionEnd;
                    phoneInput.value = phoneInput.value.slice(0, start) + digitsOnly + phoneInput.value.slice(end);
                }
            });
        }

        // ---- Show the "please fill all mandatory fields" popup whenever the client-side
        // validator blocks a submit attempt on the water connection application form. ----
        if (typeof jQuery === "undefined") {
            return;
        }

        var $form = jQuery("#aqWaterForm");
        if ($form.length === 0) {
            return;
        }

        $form.on("invalid-form.validate", function () {
            var modalEl = document.getElementById("aqValidationModal");
            if (modalEl && window.bootstrap) {
                new bootstrap.Modal(modalEl).show();
            }
        });
    });
})();
