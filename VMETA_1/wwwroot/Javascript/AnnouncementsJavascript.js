GetIssue();
var idIssue;
document.getElementById("deleteButton").addEventListener("click", async () => {

    var Issue;

    for (let i = 0; i < IssueList.length; i++) {
        if (IssueList[i].divid == idIssue) {
            Issue = IssueList[i];
            break;
        }
    }
    const options = {
        method: 'DELETE' // Metodo della richiesta
    };
    const url = "http://87.15.152.180/api/DeleteAnnouncement/" + Issue.Id;
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



});
var IssueList = []
function GetIssue() {

    fetch('http://87.15.152.180api/GetAnnouncements')
        .then(response => response.json())
        .then(data => UpdateDisplay(data))
        //.catch(error => console.error("Unable get the Problemi mannaggia"))

}


function UpdateDisplay(data) {



    for (let i = 0; i < data.length; i++) {

        var ann = new Announcement(data[i]["id"], data[i]["title"], data[i]["description"], data[i]["insertionDate"], data[i]["announcerId"], data[i]["announcerName"], data[i]["classroomYEAR"],"div"+i)

        IssueList.push(ann)


        var divo = document.createElement("div");
        divo.id = "div" + i;



        divo.setAttribute("class", "ProblemCard");
        var Title = document.createElement("div");
        Title.setAttribute("class", "ProblemCardTITLE");
        var Description = document.createElement("div");
        Description.setAttribute("class", "ProblemCardDESCRIPTION");
     

        Title.innerHTML = data[i]["title"];
    
        Description.innerHTML = data[i]["announcerName"] + " del " + data[i]["classroomYEAR"]+"° anno";
 

        var bottomcard = document.createElement("div");

        bottomcard.setAttribute("class", "BottomCard");
        bottomcard.appendChild(Description)
    

        divo.appendChild(Title);
        divo.appendChild(bottomcard);

        divo.addEventListener("click", (target) => {

            document.getElementById("content").style.display = "none";
            document.getElementById("problemVisual").style.display = "block"
            DisplayProblem(target.currentTarget.id);

        })

        document.getElementById("content").appendChild(divo);


    }


}
var issueee;
function DisplayProblem(id) {

    idIssue = id;
    for (let i = 0; i < IssueList.length; i++) {
        if (IssueList[i].divid == id) {
            issueee = IssueList[i];
            break;
        }
    }

    document.getElementById("ProblemTitle").innerHTML = issueee.Title;

 
    document.getElementById("Person").innerHTML = issueee.AnnouncerName + " del " + issueee.ClassroomYEAR+"° anno";
    

    document.getElementById("Description").innerHTML = issueee.Description;



    var butt;
    if (issueee.aiforced) {
        butt = document.createElement("button")
        butt.innerHTML = "Conferma ByPass"

        butt.addEventListener("click", () => {
            AIModify();
        });
        butt.setAttribute("class", "bottonebypass");
        document.getElementById("problemVisual").appendChild(butt);
        document.getElementById("problemVisual").style.backgroundColor = "orange";
    }

}
async function AIModify() {

    id = issueee.trueid;
    await fetch('api/ModificaAI/' + id, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer your-access-token' // If authentication is required
        },
        body: "we"
    })
        .then(response => {
            if (!response.ok) {
                // Handle error response
                return response.json().then(err => { throw err });
            }
            // Successfully updated
            return response.json();
        })
        .then(data => {
            console.log('Update successful:', data);
        })
        .catch(error => {
            console.error('Error:', error);
        });

    window.location.reload();

} 
