"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/ollamaHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (message) {
    //document.getElementById("llmresult").appendChild(li);
    if (message) { // Check if data is not null or empty
        const container = document.getElementById("llmresult");
        container.textContent += message;
    }
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    //li.textContent = `${user} says ${message}`;
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var smlouvaId = document.getElementById("smlouvaId").value;
    var numPriloha = document.getElementById("numPriloha").value;
    connection.invoke("AskLLM", smlouvaId, numPriloha).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});