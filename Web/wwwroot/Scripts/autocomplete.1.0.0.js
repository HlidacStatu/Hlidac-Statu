//autocomplete

class AutocompleteManager {
    constructor(selector, targetSearchUri, inputTagsJson, buttonId, options = {}) {
        this.selector = selector;
        this.autocompleteLastQuery = '';
        this.options = options;
        this.inputTagsJson = inputTagsJson;
        this.targetSearchUri = targetSearchUri;
        this.buttonId = buttonId;
        this.init();
    }

    init() {
        const defaultOptions = {
            plugins: ["remove_button"],
            maxItems: null,
            valueField: 'id',
            labelField: 'text',
            searchField: null,
            create: false,
            highlight: false,
            maxOptions: 9,
            loadingClass: null,
            loadThrottle: 250,
            delimiter: '░',
            render: {
                option: (item, escape) => this.renderOption(item, escape)
            },
            load: (query, callback) => this.load(query, callback),
            score: () => () => 1,
            onType: (str) => this.onType(str),
            onItemAdd: (value, $item) => this.onItemAdd(value, $item),
            onBlur: () => this.onBlur(),
            onDelete: (values) => this.onDelete(),
            onOptionRemove: (value) => this.onOptionRemove(),
            onItemRemove: (value) => this.onItemRemove(),
        };

        const config = { ...defaultOptions, ...this.options };

        $(this.selector).selectize(config);

        this.attachEvents();
    }

    renderOption(item, escape) {
        let smallClass = item.category === 'Hint' ? 'small' : '';
        let kIndexImage = item.kIndex ? `<img src="/Content/kindex/hranate/icon${escape(item.kIndex)}.svg" class="kindex" style="height: 20px; vertical-align: text-bottom; margin-right: 5px;">` : '';

        let html = `<div class="d-flex align-items-center justify-content-start">
            <div class="hs-avatar ${smallClass}">${item.imageElement}</div>
            <div class="hs-autocomplete-description ${smallClass} flex-grow-1 me-2">`;

        if (item.category === 'Hint') {
            html += `<h6 class="align-middle">${escape(item.text)} <span class="badge rounded-pill bg-secondary">${escape(item.id)}</span></h6>`;
        } else {
            html += `<h6 class="fw-bold mb-0">${kIndexImage}${escape(item.text)}</h6>
                         <small>${escape(item.description)}</small>
                         <p class="small text-muted mb-0"><i>${escape(item.type)}</i></p>`;
        }

        html += '</div><div>';

        if (item.id.startsWith('osobaid:')) {
            html += `<a href="/osoba/${escape(item.id.replace('osobaid:', ''))}" class="text-muted">
                            <i class="fa-regular fa-arrow-up-right-from-square fa-lg"></i></a>`;
        } else if (item.id.startsWith('ico:')) {
            html += `<a href="/subjekt/${escape(item.id.replace('ico:', ''))}" class="text-muted">
                            <i class="fa-regular fa-arrow-up-right-from-square fa-lg"></i></a>`;
        }

        html += '</div></div>';

        return html;
    }

    load(query, callback) {
        if (!query.length) return callback();
        $.ajax({
            url: 'https://ac.hlidacstatu.cz/autocomplete/autocomplete',
            type: 'GET',
            dataType: 'json',
            data: { q: query },
            error: function () { callback(); },
            success: (res) => {
                res.unshift({
                    id: query,
                    text: query,
                    description: '',
                    type: 'Hledat tento text (stačí zmáčknout ENTER)',
                    imageElement: '<i class="fa-solid fa-pencil"></i>'
                });

                res.forEach(function (item) {
                    item.id = item.id + '÷' + new Date().getTime();
                });
                callback(res);
            }
        });
    }

    onType(str) {
        if (str === '') {
            this.clearOptions();
            this.close();
        }
        this.autocompleteLastQuery = str;
        this.clearOptions();
    }

    onItemAdd(value, $item) {
        let trackingData = {
            selectedValue: value.split('÷')[0],
            lastQuery: this.autocompleteLastQuery,
            source: window.location.href,
            type: 'partialAutocomplete'
        };
        sendTrackingDataToAPI(trackingData);

        this.autocompleteLastQuery = '';
        this.clearOptions();
        this.close();
    }

    onBlur() {
        this.clearOptions();
        this.close();
    }

    onDelete() {
        this.clearOptions();
        this.close();
    }

    onOptionRemove() {
        this.clearOptions();
        this.close();
    }

    onItemRemove() {
        this.clearOptions();
        this.close();
    }

    attachEvents() {
        $(this.selector + "-selectized").on('keypress', (event) => {
            if (event.which === 13 && $(this.selector + "-selectized")[0].value.length === 0) {
                this.search();
            }
        });

        $(this.buttonId).click(() => this.search());

        this.recreateSelectedTags();
    }

    recreateSelectedTags() {
        if (this.inputTagsJson) {
            var initialItems = JSON.parse(this.inputTagsJson);

            var selectizeControl = $(this.selector)[0].selectize;
            var counter = 0;

            initialItems.forEach(function (item) {
                var uniqueId = item.Id + '÷' + counter++;
                selectizeControl.addOption({ id: uniqueId, text: item.Text, imageElement: item.ImageElement, description: item.Description, type: item.Type, kIndex: item.KIndex, category: item.Category });
                selectizeControl.addItem(uniqueId);
            });
        }
    }

    search() {
        $('#searchSpiner').css('display', 'inline-block');
        var selectedValues = $(this.selector)[0].selectize.getValue().map(item => item.split('÷')[0]);

        var qs = selectedValues.join(' ');
        var qtl = selectedValues.map(item => item.length).join(',');

        let trackingData = {
            selectedValues: selectedValues,
            source: window.location.href,
            type: 'fullAutocomplete'
        };
        sendTrackingDataToAPI(trackingData);

        var encodedQs = encodeURIComponent(qs);
        var newUrl = this.targetSearchUri + `?q=${encodedQs}&qtl=${qtl}&u=1`;
        window.location.href = newUrl;
    }

    clearOptions() {
        $(this.selector)[0].selectize.clearOptions();
    }

    close() {
        $(this.selector)[0].selectize.close();
    }
}

function sendTrackingDataToAPI(data) {
    try {
        const url = "https://api.hlidacstatu.cz/api/v2/tracking";

        // Prepare the data as a Blob
        const blobData = new Blob([JSON.stringify(data)], { type: 'application/json' });

        // Send the data using the Beacon API
        const success = navigator.sendBeacon(url, blobData);

        if (!success) {
            console.error("Data could not be sent with sendBeacon");
        }
    } catch (e) {
        // If an exception occurs, do nothing (fire and forget)
    }
}

/**
 * A generic typeahead search component.
 *
 * Options:
 * - container: DOM element in which to render the component.
 * - labelText: (optional) Text for the label.
 * - placeholder: (optional) Placeholder text for the input.
 * - endpoint: Either a string (base URL) or a function(query) that returns a Promise resolving to an array.
 * - onSelect: (optional) Callback function invoked when a suggestion is selected.
 */
/*
how to use
document.addEventListener("DOMContentLoaded", function() {
    const container = document.getElementById("company-search-container");
    const searchComponent = new SimpleAutocomplete({
    container: container,
    labelText: "Poskytovatel dotace",
    placeholder: "Najít úřad/firmu",
    endpoint: "/dotace/GetPoskytovatele/", // The component will append the query string.
    inputStyle: "height:2em",
    inputName: "poskytovatel"
    });
});

*/
class SimpleAutocomplete {
    constructor(options) {
        // Throw an exception if the required container option is not provided.
        if (!options || !options.container) {
            throw new Error("SimpleAutocomplete: The 'container' option is required.");
        }
        this.container = options.container;
        this.labelText = options.labelText || "Hledat:";
        this.placeholder = options.placeholder || "Začněte psát...";
        this.endpoint = options.endpoint; // e.g. "/kindex/findcompany/" or a function(query){...}
        this.onSelect = options.onSelect || this.defaultOnSelect.bind(this);
        this.inputClass = options.inputClass || "form-control";
        this.inputStyle = options.inputStyle || "";
        this.selectedIndex = -1;
        this.suggestions = [];
        this.inputId = options.inputName || "typeahead-search-input-" + Math.random().toString(36).substr(2, 9);
        this.render();
        this.lastKeyPressTime = 0;
    }

    render() {
        // Create a wrapper with relative positioning.
        this.wrapper = document.createElement("div");
        this.wrapper.style.position = "relative";

        // Create and append label.
        this.label = document.createElement("label");
        // Create a unique id for the input.
        this.label.setAttribute("for", this.inputId + "-text");
        this.label.textContent = this.labelText;
        this.label.className = "form-label";
        this.wrapper.appendChild(this.label);

        // Create and append the text input.
        this.input = document.createElement("input");
        this.input.type = "text";
        this.input.placeholder = this.placeholder;
        this.input.id = this.inputId + "-text";
        this.input.className = this.inputClass;
        this.input.style = this.inputStyle;
        this.wrapper.appendChild(this.input);

        // Create and append the hidden input.
        this.hiddenInput = document.createElement("input");
        this.hiddenInput.id = this.inputId;
        this.hiddenInput.type = "hidden";
        this.hiddenInput.name = this.inputId;
        this.wrapper.appendChild(this.hiddenInput);

        // Create and append the spinner.
        this.spinner = document.createElement("div");
        this.spinner.className = "loading-spinner";
        this.wrapper.appendChild(this.spinner);

        // Create and append the suggestions container.
        this.suggestionsContainer = document.createElement("div");
        this.suggestionsContainer.id = "suggestions-container";
        this.wrapper.appendChild(this.suggestionsContainer);

        // Finally, add the wrapper to the provided container.
        this.container.appendChild(this.wrapper);

        // Attach event listeners.
        this.input.addEventListener("input", (e) => this.handleInput(e));
        this.input.addEventListener("keydown", (e) => this.handleKeyPressed(e));
        this.input.addEventListener("keyup", (e) => this.handleKeyPressed(e));
    }

    showSpinner() {
        this.spinner.style.display = "block";
    }

    hideSpinner() {
        this.spinner.style.display = "none";
    }

    handleInput(e) {
        const query = e.target.value.trim();
        if (!query) {
            this.clearSuggestions();
            return;
        }
        this.fetchSuggestions(query);
    }

    fetchSuggestions(query) {
        this.showSpinner();
        // If endpoint is a function, use it directly.
        if (typeof this.endpoint === "function") {
            this.endpoint(query)
                .then(data => {
                    this.hideSpinner();
                    this.renderSuggestions(data);
                })
                .catch(err => {
                    this.hideSpinner();
                    console.error(err);
                });
        } else {
            // Otherwise, assume endpoint is a URL string to which we append the query.
            const url = this.endpoint + encodeURIComponent(query);
            fetch(url)
                .then(response => response.json())
                .then(data => {
                    this.hideSpinner();
                    this.renderSuggestions(data);
                })
                .catch(err => {
                    this.hideSpinner();
                    console.error(err);
                });
        }
    }

    renderSuggestions(data) {
        this.clearSuggestions();
        this.suggestions = data;
        this.selectedIndex = -1;
        data.forEach((item, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = item.text;
            button.value = item.id;
            button.className = "whisp";
            button.addEventListener("keydown", (e) => this.handleKeyPressed(e));
            button.addEventListener("keyup", (e) => this.handleKeyPressed(e));

            // When clicked, select this suggestion.
            button.addEventListener("click", () => this.selectSuggestion(index));
            this.suggestionsContainer.appendChild(button);
        });
    }

    clearSuggestions() {
        this.suggestionsContainer.innerHTML = "";
        this.suggestions = [];
        this.selectedIndex = -1;
    }


     
    handleKeyPressed(e) {
        const now = Date.now();
        if (this.lastKeyPressTime && now - this.lastKeyPressTime < 20) {
            return;
        }
        this.lastKeyPressTime = now;


        // Get the list of suggestion buttons.
        const suggestionButtons = Array.from(this.suggestionsContainer.querySelectorAll("button.whisp"));
        if (suggestionButtons.length === 0) return;


        switch (e.keyCode) {
            case 38: // Up arrow.
                e.preventDefault();
                if (this.selectedIndex > 0) {
                    this.selectedIndex--;
                } else {
                    this.selectedIndex = 0;
                }
                suggestionButtons[this.selectedIndex].focus({ focusVisible: true });
                this.lastKeyPressTime = this.lastKeyPressTime + 130; //don't allow up to ofter
                break;
            case 40: // Down arrow.
                e.preventDefault();
                if (this.selectedIndex < suggestionButtons.length - 1) {
                    this.selectedIndex++;
                } else {
                    this.selectedIndex = suggestionButtons.length - 1;
                }
                suggestionButtons[this.selectedIndex].focus({ focusVisible: true });
                this.lastKeyPressTime = this.lastKeyPressTime + 130; //don't allow down to ofter
                break;
            case 27: // Escape.
                this.clearSuggestions();
                this.input.value = "";
                this.hiddenInput.value = "";

                break;
            default:
                break;
        }
    }

    selectSuggestion(index) {
        if (index >= 0 && index < this.suggestions.length) {
            const selectedItem = this.suggestions[index];
            // Set the input value to the selected name.
            this.input.value = selectedItem.text;
            this.hiddenInput.value = selectedItem.id;

            this.clearSuggestions();
            this.showSpinner();
            // Invoke the onSelect callback.
            this.onSelect(selectedItem);
        }
    }

    defaultOnSelect(item) {
        // Default action: navigate to a detail page.
        //window.location.href = `/kindex/detail/${item.ico}`;

        this.hideSpinner();
    }
}