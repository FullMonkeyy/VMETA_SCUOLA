document.getElementById("sendmessage").addEventListener("click", async () => {

    SendRequest();

});

async function SendRequest() {

    var title = document.getElementById("messagetitle").value;
    var body = document.getElementById("message").value;
    var destination
    if (document.getElementById("radioEveryone").checked) {

        destination = "E"
    }
    else if (document.getElementById("radioRappresentanti").checked) {

        destination = "R"
    }
    else
        destination = document.getElementById("selectionannate").value

    var SendRM = new RequestSendMessage(title, body, destination);
    

    await fetch("api/SendMessageALL", {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(SendRM)
    })
    .then(response => {
        if (response.ok) {
            // La richiesta è andata a buon fine (status code 2xx)
            console.log("La richiesta POST è andata a buon fine.");
        } else {
            // La richiesta ha avuto problemi (status code diverso da 2xx)
            console.error("Si è verificato un problema durante la richiesta POST.");
        }
    })
        .catch(error => {
            // Si è verificato un errore durante l'invio della richiesta
            console.error("Si è verificato un errore durante l'invio della richiesta POST:", error);
        });
    window.location.reload();


}