"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ollamaHub").build();

//Disable the send button until connection is established.
document.getElementById("send2LLM").disabled = true;

connection.on("ReceiveMessage", function (message) {
    //document.getElementById("llmresult").appendChild(li);
    if (message) { // Check if data is not null or empty
        document.getElementById("llm_loading").style.display = 'none';

        const container = document.getElementById("pre_llmresult");
        container.textContent += message;
    }
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    //li.textContent = `${user} says ${message}`;
});


connection.on("ReceiveProgress", function (message) {
    //document.getElementById("llmresult").appendChild(li);
    if (message) { // Check if data is not null or empty
        console.log(message)
        //document.getElementById("llm_loading_progressbar_body")
        var progressBar = document.getElementById('llm_loading_progressbar_body');
        var roundedValue = (message.progress * 100).toFixed(1);
        progressBar.style.width = roundedValue + '%';
        progressBar.innerText = roundedValue + '% ' + message.message;
        progressBar.setAttribute('aria-valuenow', roundedValue);

    }
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    //li.textContent = `${user} says ${message}`;
});

connection.on("ReceiveJsonSummaryMessage", function (message) {
    //document.getElementById("llmresult").appendChild(li);
    if (message) { // Check if data is not null or empty

        document.getElementById("llm_loading").style.display = 'none';


        const pre_content = document.getElementById("pre_llmresult");
        pre_content.style.display = 'none';

        const contentDiv = document.getElementById('html_llmresult');
        contentDiv.style.display = 'block';

        const jsonData = message;

        const ul = document.createElement('ul');

        jsonData.forEach(item => {
            const li = document.createElement('li');
            const title = document.createElement('strong');
            if (item.titulek) {
                title.textContent = item.titulek + ": ";
            }
            li.appendChild(title);

            const description = document.createElement('div');
            description.textContent = item.bod;
            li.appendChild(description);

            ul.appendChild(li);
        });

        contentDiv.appendChild(ul);    }
});

connection.start().then(function () {
    document.getElementById("send2LLM").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("send2LLM").addEventListener("click", function (event) {
    var smlouvaId = document.getElementById("LLMsmlouvaId").value;
    var instruction = document.getElementById("LLMinstruction").value;
    var pocetbodu = document.getElementById("LLMpocetbodu").value;
    document.getElementById("pre_llmresult").value = '';
    document.getElementById("llm_loading").style.display = 'block';
    document.getElementById('html_llmresult').innerHTML = '';

    //var numPriloha = document.getElementById("numPriloha").value;
    connection.invoke("AskLLM", instruction, smlouvaId, "",pocetbodu).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});