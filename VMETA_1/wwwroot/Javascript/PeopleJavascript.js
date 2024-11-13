GetStudents();

document.getElementById("inp_src_cognomi").addEventListener("input", () => {

    if (document.getElementById("inp_src_cognomi").value!="")
        GetStudentsCognome();
    else
        GetStudents();

});
function GetStudentsCognome() {

    fetch('api/SearchStudentsCognome/' + document.getElementById("inp_src_cognomi").value)
        .then(response => response.json())
        .then(data => DisplayStudents(data))
        .catch(error => console.error("Unable get the students mannaggia"))

}
function GetStudents() {

    fetch('api/GetStudents')
        .then(response => response.json())
        .then(data => DisplayStudents(data))
        .catch(error => console.error("Unable get the students mannaggia"))

}
function GetClassroom() {


    fetch('api/GetClassrooms')
        .then(response => response.json())
        .then(data => addClassrooms( data))
        .catch(error => console.error("Unable get the students mannaggia"))

}
function addClassrooms(data) {

    var container = document.getElementById("selectionClassroom");
    for (let i = 0; i < data.length; i++) {

        var opt = document.createElement("option");
        opt.value = i+1;
        opt.innerHTML = data[i]["year"] + data[i]["section"] + data[i]["specialization"];
        container.appendChild(opt);
    }

}
function DisplayStudents(Data) {

    var Container = document.getElementById("content");
    Container.innerHTML = "";
    for (let i = 0; i < Data.length; i++) {

        var divo = document.createElement("div");
        divo.setAttribute("class", "student");
        var icon = document.createElement("img")
        icon.setAttribute("class", "innerIcon")
     
        divo.id = Data[i]["name"] + "_" + Data[i]["surname"] 
        if (Data[i]["isJustStudent"]) {
            icon.src = '../CSS/Assets/personicon.png'
            divo.id +=" student"

        } else {
            icon.src = '../CSS/Assets/star.png'
            divo.id += " rappresentante"
        };

        divo.appendChild(icon)
        divo.innerHTML += "<span class='innerSpanStudent'>" + Data[i]["surname"] + " " + Data[i]["name"] + " " + Data[i]["classroom"] + "</span>"
    

        if (!Data[i]["isRegistred"]) divo.style.backgroundColor = "green";

        var img = document.createElement("img");
        img.src = "../CSS/Assets/cestino.png"
        img.setAttribute("class", "cestino");
    
        img.id = Data[i]["name"] + " " + Data[i]["surname"] ;
        img.addEventListener("click", (event) => {
            const options = {
                method: 'DELETE' // Metodo della richiesta
            };
            const url = "/api/DeletePerson/" + event.target.id;
            // Effettua la richiesta utilizzando fetch()
            fetch(url, options)
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Errore nella richiesta DELETE');
                    }
                    console.log('Libro rimosso con successo.');
                    window.location.reload();
                    // Puoi gestire la risposta qui se necessario
                })
                .catch(error => {
                    console.error('Si è verificato un errore:', error);
                });

        })
        divo.appendChild(img)
      
        divo.addEventListener("click", async (event) => {

            tmp1=event.target.id.split(" ")
            if (tmp1[1] =="rappresentante")
                await ChangeRole(tmp1[0], false)
            else await ChangeRole(tmp1[0], true)
            window.location.reload();
        })
        Container.appendChild(divo);


    }

    var divo = document.createElement("div");
    divo.id = "AddStudentDiv";
    divo.innerHTML = "Add Students +";
    divo.addEventListener("click",AddStudentProcess)
    Container.appendChild(divo);


}
async function ChangeRole(id,isjuststudent) {
    var endpoint
    if (isjuststudent) {
        endpoint = "api/MakeRappresentante/" + id
        await fetch(endpoint, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            }
        });
    }
    else {
        endpoint = "api/MakeStudent/" + id
        await fetch(endpoint, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            }
        });
    }


}
function AddStudentProcess() {

    GetClassroom();

    document.getElementById("CreatingStudent").style.display = "block";

}
document.getElementById("buttonRegister").addEventListener("click", sendNewPerson);
async function sendNewPerson() {

    var FirstName = document.getElementById("firstnameInput").value;
    var LasttName = document.getElementById("lastnameInput").value;
    var Email = document.getElementById("emailInput").value;
    var PhoneNumber = document.getElementById("phonenumberInput").value;
    var selector = document.getElementById("selectionClassroom");
    var ClassRoom = selector[document.getElementById("selectionClassroom").value - 1].innerHTML;
    var TelegramCode = document.getElementById("TelegramCode").value;
    var isStudent = document.getElementById("CheckBoxlInput")["checked"];

   
    TelegramCode = TelegramCode.toUpperCase();
    var Pers = new Person(FirstName, LasttName, ClassRoom, "null", Email, PhoneNumber,TelegramCode,isStudent);


    url = " api/SendPerson";  

    await fetch(url, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(Pers)
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


async function Elimina(nome) {


    const options = {
        method: 'DELETE' // Metodo della richiesta
    };
    const url = "/api/DeletePerson/" + nome;
    // Effettua la richiesta utilizzando fetch()
    fetch(url, options)
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