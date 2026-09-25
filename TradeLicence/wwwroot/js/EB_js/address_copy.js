document.addEventListener("DOMContentLoaded", function () {
    const sameYes = document.getElementById("sameYes");
    const sameNo = document.getElementById("sameNo");

    function copyAddress() {
        if (sameYes && sameYes.checked) {
            document.getElementById("CommDoorNo").value = document.getElementById("SiteDoorNo").value;
            document.getElementById("CommStreet").value = document.getElementById("SiteStreet").value;
            document.getElementById("CommArea").value = document.getElementById("SiteArea").value;
            document.getElementById("CommRegion").value = document.getElementById("SiteDistrict").value;
            document.getElementById("CommPincode").value = document.getElementById("SitePincode").value;
        }
    }

    if (sameYes) sameYes.addEventListener("change", copyAddress);
    if (sameNo) {
        sameNo.addEventListener("change", function () {
            if (this.checked) {
                document.getElementById("CommDoorNo").value = "";
                document.getElementById("CommStreet").value = "";
                document.getElementById("CommArea").value = "";
                document.getElementById("CommRegion").value = "";
                document.getElementById("CommPincode").value = "";
            }
        });
    }
});