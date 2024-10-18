document.getElementById("send").addEventListener("click", Send);

fetch('api/Clear')
    .then(response => response.json())
    .catch(error => console.error("Unable get the shelves mannaggia"))

function Send() {

    var message = document.getElementById("message").value;

    const sendmess = new Message("User", message);

    console.log(message);
    var boxchat = document.getElementById("content");
    boxchat.innerHTML ="";
    url = "api/SendMessage";

    fetch('api/Clear')
        .then(response => response.json())
        .catch(error => console.error("Unable get the shelves mannaggia"))


    fetch(url, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(sendmess)
    })
        .then(response => {
            if (response.ok) {
                // La richiesta è andata a buon fine (status code 2xx)
                console.log("La richiesta POST è andata a buon fine.");
                document.getElementById("popup").style.display = "block";
            } else {
                // La richiesta ha avuto problemi (status code diverso da 2xx)
                console.error("Si è verificato un problema durante la richiesta POST.");
            }
        })
        .catch(error => {
            // Si è verificato un errore durante l'invio della richiesta
            console.error("Si è verificato un errore durante l'invio della richiesta POST:", error);
        });


}

setInterval(SendReq,10)

function SendReq() {

    fetch('api/GetResponse')
        .then(response => response.json())
        .then(data => UpdateDisplay(data))
        .catch(error => console.error("Unable get the shelves mannaggia"))
  


}

function UpdateDisplay(data) {

    var boxchat = document.getElementById("content");
    boxchat.innerHTML = data["content"];


}



//codice per la registrazione audio
var startButton = document.getElementById("mic");
var stopButton = document.getElementById("mic2");
var playButton = document.getElementById("mic3");
let toogle=true
let audioRecorder;
let audioChunks = [];
navigator.mediaDevices.getUserMedia({ audio: true })
    .then(stream => {

        // Initialize the media recorder object
        audioRecorder = new MediaRecorder(stream);
      
        // dataavailable event is fired when the recording is stopped
        audioRecorder.addEventListener('dataavailable', e => {
            audioChunks.push(e.data);
        });

        // start recording when the start button is clicked
        startButton.addEventListener('click', () => {
         
                audioChunks = [];
                audioRecorder.start(); 
            stopButton.style.display = "block";
            startButton.style.display = "none";
        });

        // stop recording when the stop button is clicked
        stopButton.addEventListener('click', () => {
   
            audioRecorder.stop();      
            stopButton.style.display = "none";
            playButton.style.display = "block";
        });

        // play the recorded audio when the play button is clicked
        playButton.addEventListener('click', () => {
            const blobObj = new Blob(audioChunks, { type: 'audio/webm' });
            SpedisciVoce(blobObj).then(
                () => {
                              const audioUrl = URL.createObjectURL(blobObj);
                    const audio = new Audio(audioUrl);
                    audio.play();
                
                    startButton.style.display = "block";
                    playButton.style.display = "none";
                }
            )
  
        });
     
    }).catch(err => {

        // If the user denies permission to record audio, then display an error.
        console.log('Error: ' + err);
    });

async function SpedisciVoce(blobObj) {

    const formData = new FormData();
    formData.append('file', blobObj, 'audio.webm');  // 'file' deve corrispondere al parametro 'IFormFile file' in ASP.NET

    await fetch('api/SendVoice', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => console.log('File caricato:', data.filePath))
        .catch(error => console.error('Errore:', error));


}