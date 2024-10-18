
document.getElementById("buttonStart").addEventListener("click", Active);

async function Active() {


    await fetch('api/Start')
        .then(response => response.json())
        .then(data => Logga(data))
        .catch(error => console.error("Unable get the shelves mannaggia"))
    window.location.href="../VanessaChat.html"
}
function Logga(dati) {

    console.log(dati);

}