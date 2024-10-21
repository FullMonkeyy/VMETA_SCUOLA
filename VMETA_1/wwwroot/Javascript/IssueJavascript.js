GetIssue();
var idIssue;
document.getElementById("deleteButton").addEventListener("click", async () => {

    var Issue;

    for (let i = 0; i < IssueList.length; i++) {
        if (IssueList[i].id == idIssue) {
            Issue = IssueList[i];
            break;
        }
    }
    const options = {
        method: 'DELETE' // Metodo della richiesta
    };
    const url = "/api/DeleteIssue/" + Issue.title;
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
var IssueList=[]
function GetIssue() {

    fetch('api/GetProblems')
        .then(response => response.json())
        .then(data => UpdateDisplay(data))
        .catch(error => console.error("Unable get the Problemi mannaggia"))

}


function UpdateDisplay(data) {

   

    for (let i=0; i < data.length; i++) {

        IssueList.push(new Problem(data[i]["classroom"], data[i]["description"], data[i]["person"], data[i]["secret"], data[i]["solution"], data[i]["title"], data[i]["category"], data[i]["aiForced"] ,"div" + i,data[i]["id"]))


        var divo = document.createElement("div");
        divo.id = "div" + i;



        divo.setAttribute("class", "ProblemCard");
        var Title = document.createElement("div");
        Title.setAttribute("class", "ProblemCardTITLE");
        var Description = document.createElement("div");
        Description.setAttribute("class", "ProblemCardDESCRIPTION");
        var category = document.createElement("div");
        category.setAttribute("class", "ProblemCardDESCRIPTION")
        category.innerHTML = data[i]["category"];

        Title.innerHTML = data[i]["title"];

        if (data[i]["isStudent"] == "true")
            Description.innerHTML = data[i]["person"] + " " + data[i]["classroom"];
        else
            Description.innerHTML = "Problema della classe " + data[i]["classroom"];

        var bottomcard = document.createElement("div");

        bottomcard.setAttribute("class", "BottomCard");
        bottomcard.appendChild(Description)
        bottomcard.appendChild(category)


        if (data[i]["aiForced"]) {
            divo.style.backgroundColor = "yellow";
          
        }
                
        divo.appendChild(Title);
        divo.appendChild(bottomcard);     

        divo.addEventListener("click", (target) => {

            document.getElementById("content").style.display = "none";
            document.getElementById("problemVisual").style.display="block"
            DisplayProblem(target.currentTarget.id);

        })

        document.getElementById("content").appendChild(divo);


    }


}
var issueee;
function DisplayProblem(id) {

    idIssue = id;
    for (let i = 0; i < IssueList.length; i++) {
        if (IssueList[i].id == id) {
            issueee = IssueList[i];
            break;
        }
    }

    document.getElementById("ProblemTitle").innerHTML = issueee.title;

    if (issueee.secret=="false")
        document.getElementById("Person").innerHTML = issueee.person;
    else document.getElementById("Person").innerHTML = "Rappresentante della classe " + issueee.classroom;

    document.getElementById("Category").innerHTML = issueee.category;


    document.getElementById("Description").innerHTML = issueee.description;


    document.getElementById("Solution").innerHTML = issueee.solution;
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
    await fetch('api/ModificaAI/'+id, {
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
