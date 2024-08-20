//autocomplete

class AutocompleteManager {
    constructor(selector, targetSearchUri, inputTagsJson, options = {}) {
        this.selector = selector;
        this.autocompleteLastQuery = '';
        this.options = options;
        this.inputTagsJson = inputTagsJson;
        this.targetSearchUri = targetSearchUri;
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
        $(this.selector).on('keypress', (event) => {
            if (event.which === 13 && $(this.selector)[0].selectize.getValue().length === 0) {
                this.search();
            }
        });

        $('#getValuesBtn').click(() => this.search());

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
