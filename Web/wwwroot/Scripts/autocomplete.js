// initialize Tagify
var search_parsed = '';
var searchform = document.querySelector('#new-search-input2'); // just an example

var input = document.querySelector('#new-search-input2-q');
// init Tagify script on the above inputs
var tagify = new Tagify(input, {
    keepInvalidTags: true,
    duplicates: false,
    templates: {
        dropdownItem: suggestionItemTemplate
    },
    whitelist: [],
    callbacks: {
        //add: console.log,  // callback when adding a tag
        //remove: console.log,   // callback when removing a tag
        change: onTagifyChange,
        input: onTagifyInput,
        keydown: onTagifyKeyDown
    }
});
var controller; // for aborting the call

tagify.on("dropdown:show", onSuggestionsListUpdate);
//tagify.on('input', onTagifyInput);
//tagify.on('keydown', onTagifyKeyDown);

function onTagifyFixQuery(e) {
    input.value = search_parsed;
    return true;
}

function onTagifyKeyDown(e) {
    if (e.detail.originalEvent.key == 'Enter' &&         // "enter" key pressed
        !tagify.state.inputText &&  // assuming user is not in the middle oy adding a tag
        !tagify.state.editing       // user not editing a tag
    ) {
        setTimeout(() => { onTagifyFixQuery(); searchform.submit(); });  // put some buffer to make sure tagify has done with whatever, to be on the safe-side
    }
}

// ES2015 argument destructuring
function onSuggestionsListUpdate({ detail: suggestionsElm }) {
    console.log(suggestionsElm)
}
// ES2015 argument destructuring
function onSuggestionsListUpdateFull(e) {
    console.log(e);
}

function onTagifyChange(e) {
    if (e === undefined) {
        return;
    }
    // string [{"value":"test"}', '{"value":"test2"}] to JSON Object
    if (e.detail.value) {
        var tags = JSON.parse(e.detail.value);
        // Converts into a simple array ["test", "test2"], then convert to string "test,test2"
        search_parsed = tags.map(item => {
            if (item.id) return item.id;
            else return item.value;
        })
            .join(' ');
    }
    else search_parsed = '';
};

// A good place to pull server suggestion list accoring to the prefix/value
function onTagifyInput(e) {
    var value = e.detail.value
    tagify.whitelist = null // reset the whitelist

    // https://developer.mozilla.org/en-US/docs/Web/API/AbortController/abort
    controller && controller.abort()
    controller = new AbortController()

    // show loading animation and hide the suggestions dropdown
    tagify.loading(true).dropdown.hide()

    fetch('/beta/autocomplete/?type=query&q=' + value, { signal: controller.signal })
        .then(RES => RES.json())
        .then(function (inp) {
            // update inwhitelist Array in-place
            var newWhiteList = inp.map(function (i) {
                return {
                    id: i.id,
                    value: i.text,
                    searchBy: i.description,
                    html: encodeURIComponent(i.imageElement),
                    type: i.type,
                    description: i.description,
                    //    priority: i.priority
                }
            })
            tagify.whitelist = newWhiteList;
            tagify.loading(false).dropdown.show(value) // render the suggestions dropdown
        })
}

function suggestionItemTemplate(tagData) {
    return `
        <div class='tagify__dropdown__item ${tagData.class ? tagData.class : ""}' tabindex="0" role="option" ${this.getAttributes(tagData)}>
        <div class='clearfix'>
            <div class='tagify__dropdown__item__avatar'>${decodeURIComponent(tagData.html)}</div>
            <div class='tagify__dropdown__item__info'>
                <div class='tagify__dropdown__item__title'>${tagData.value}</div>
                <div class='tagify__dropdown__item__descr'>${tagData.description}</div>
                <div class='tagify__dropdown__item__stat'><i>${tagData.type}</i></div>
            </div>
        </div>
        </div>`
}
