
GetPools();

function GetPools() {

    fetch('api/GetPools')
        .then(response => response.json())
        .then(data => UpdateDisplay(data))
        .catch(error => console.error("Unable get the Problemi mannaggia"))

}
PoolsList = []

function UpdateDisplay(data) {

    var Container = document.getElementById("content");
    let divetto;
    let titlediv;
    let descipritiondiv;
    let votesDiv;
    let votediv
    for (let i = 0; i < data.length; i++) {


        PoolsList.push(new Pool(data[i]["titolo"], data[i]["descrizione"], data[i]["votes"], null,i,data[i]["id"]))
        divetto = document.createElement("div");
        divetto.setAttribute("class", "poolCSS");

        titlediv=document.createElement("div")
        titlediv.innerHTML = data[i]["titolo"]
        titlediv.setAttribute("class", "poolTITLECSS");

        
        descipritiondiv = document.createElement("div")
        descipritiondiv.innerHTML = data[i]["descrizione"]
        descipritiondiv.setAttribute("class", "poolDESCRIPTIONCSS");
        
        votesDiv = document.createElement("div")
        votesDiv.setAttribute("class", "poolVOTESLISTCSS");
        voteslist = data[i]["votes"]
        
        for (let j = 0; j < voteslist.length; j++) {

            votediv = document.createElement("div")
            votediv.innerHTML = data[i]["votes"][j]["title"] +" ["+ data[i]["votes"][j]["votes"]+"]"
            votediv.setAttribute("class", "innersinglevote")
            votesDiv.appendChild(votediv)
         
        }
      
        divetto.appendChild(titlediv)
        //divetto.appendChild(descipritiondiv)
        divetto.appendChild(votesDiv)      
        divetto.id="div_"+i
        divetto.addEventListener("click", (target) => {

            document.getElementById("content").style.display = "none";
            document.getElementById("problemVisual").style.display = "block"
            //document.getElementById("main").style.height = "1500px"
            //document.getElementById("main").style.width = "1583px"
            //document.getElementById("main").style.pa
            DisplayProblem(target.currentTarget.id);

        })
       
        Container.appendChild(divetto);


    }


    var divo = document.createElement("div");
    divo.id = "AddStudentDiv";
    divo.innerHTML = "CREATE NEW POOL";
    divo.addEventListener("click", AddPoolProcess)
    Container.appendChild(divo);




}
var trueid;
function DisplayProblem(id) {

    let correctId = id.split("_")[1]
    var pool;

    for (let i = 0; i < PoolsList.length; i++) {

        if (PoolsList[i].id == correctId) {
            console.log("Div trovato");
            pool = PoolsList[i];
            trueid = pool.trueid;
            break;
        }

    }

    document.getElementById("ProblemTitle").innerHTML = pool.title;
    document.getElementById("Description").innerHTML = pool.description;

    anychart.onDocumentReady(function () {
        // add the data

        let arr1 = []

        for (let j = 0; j < pool.options.length; j++) {

            let arr2 = []
            arr2.push(pool.options[j]["title"], pool.options[j]["votes"])
            arr1.push(arr2)

        }


        let data = anychart.data.set(arr1);
        // create a pie chart with the data
        let chart = anychart.pie(data);
        // set the chart title
        chart.title("IPL Winnership Over 16 Seasons");
        // set container id for the chart
        chart.container("Solution");

        chart
            .tooltip()
            .format("Percent of total wins: {%PercentValue}{decimalsCount: 1}%");
        // initiate chart drawing
        chart.draw();
    });
   


}


function AddPoolProcess() {
    document.getElementById("CreatingStudent").style.display = "block";

}
document.getElementById("buttonRegister").addEventListener("click", async () => {

    var titolo = document.getElementById("firstnameInput").value
    var descrizione = document.getElementById("textareaform").value

    var listaopzioni = document.getElementById("textareaOPTIONSform").value.split("-")

    var justRappr = document.getElementById("CheckBoxlInput")["checked"];
    console.log("ivan finocchio")

    var NuovoPOOL = new Pool(titolo, descrizione, listaopzioni, justRappr, 0);

    let url ="api/SendPool"

    await fetch(url, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(NuovoPOOL)
    }).then(response => {
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

    setTimeout(location.reload(), 1000);
});

document.getElementById("buttonConferming").addEventListener("click", () => {

    Elimina(trueid);

});



async function Elimina(id) {


    const options = {
        method: 'DELETE' // Metodo della richiesta
    };
    const url = "api/DeletePool/" + id;
    // Effettua la richiesta utilizzando fetch()
    await fetch(url, options)
        .then(response => {
            if (!response.ok) {
                throw new Error('Errore nella richiesta DELETE');
            }
            console.log('Libro rimosso con successo.');
            // Puoi gestire la risposta qui se necessario
        })
        .catch(error => {
            console.error('Si è verificato un errore:', error);
        });

    setTimeout(location.reload(), 1000);



}

