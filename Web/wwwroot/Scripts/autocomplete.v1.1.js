function MakeAutocomplete(elementSel) {
    // initialize Tagify
    var search_parsed = '';
    var searchform = document.querySelector(elementSel); // '#new-search-input2'

    var input = document.querySelector(elementSel + '-q');
    var btn = document.querySelector(elementSel + '-btn');
    btn.onclick = function (e) { onTagifyFixQuery(input) };
    btn.onsubmit = function (e) { onTagifyFixQuery(input) };

    // init Tagify script on the above inputs
    var tagify = new Tagify(input, {
        keepInvalidTags: true,
        duplicates: false,
        tagTextProp: 'text',
        pasteAsTags: false,
        templates: {
            tag: tagItemTemplate,
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
    tagify.on('dropdown:select', debugMe);
    tagify.on('add', debugMe);
    //tagify.on('keydown', onTagifyKeyDown);


    function onTagifyKeyDown(e) {
        if (e.detail.originalEvent.key == 'Enter' &&         // "enter" key pressed
            !tagify.state.inputText &&  // assuming user is not in the middle oy adding a tag
            !tagify.state.editing       // user not editing a tag
        ) {
            setTimeout(() => { onTagifyFixQuery(input); searchform.submit(); });  // put some buffer to make sure tagify has done with whatever, to be on the safe-side
        }
    }

    // ES2015 argument destructuring
    function debugMe(e) {
        console.log(e);
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
        var qvalue = e.detail.value;
        if (!qvalue) {
            tagify.whitelist = [] // reset the whitelist
            return;
        }
        if (qvalue.length <=2) {
            tagify.whitelist = [] // reset the whitelist
            return;
        }

        tagify.whitelist = null // reset the whitelist

        // https://developer.mozilla.org/en-US/docs/Web/API/AbortController/abort
        controller && controller.abort()
        controller = new AbortController()

        // show loading animation and hide the suggestions dropdown
        tagify.loading(true).dropdown.hide()
        try {

            fetch('/api/autocomplete?type=query&q=' + qvalue, { signal: controller.signal })
                .then(RES => RES.json())
                .then(function (inp) {
                    // update inwhitelist Array in-place
                    var newWhiteList = inp.map(function (i,n) {
                        return {
                            id: i.id,
                            value: n+1,
                            text: i.text,
                            searchBy: qvalue,
                            html: encodeURIComponent(i.imageElement),
                            type: i.type,
                            description: i.description,
                            //    priority: i.priority
                        }
                    })
                    tagify.whitelist = newWhiteList;
                    tagify.loading(false).dropdown.show(qvalue) // render the suggestions dropdown
                })
        } catch (err) {

        }

    }
    //${tagData.html ? decodeURIComponent(tagData.html) : ''}
    function tagItemTemplate(tagData) {
        try {
            return `<tag title='${tagData.text}' contenteditable='false' spellcheck="false" class='tagify__tag ${tagData.class ? tagData.class : ""}' ${this.getAttributes(tagData)}>
                        <x title='remove tag' class='tagify__tag__removeBtn'></x>
                        <div class='tagify__tag-item'>
                            <span class='tagify__tag-text'>${tagData.text}</span>
                        </div>
                    </tag>`
        }
        catch (err) { }
    }


    function suggestionItemTemplate(tagData) {
        return `
        <div class='tagify__dropdown__item ${tagData.class ? tagData.class : ""}' tabindex="0" role="option" ${this.getAttributes(tagData)}>
        <div class='clearfix'>
            <div class='tagify__dropdown__item__avatar'>${decodeURIComponent(tagData.html)}</div>
            <div class='tagify__dropdown__item__info'>
                <div class='tagify__dropdown__item__title'>${tagData.text}</div>
                <div class='tagify__dropdown__item__descr'>${tagData.description}</div>
                <div class='tagify__dropdown__item__stat'><i>${tagData.type}</i></div>
            </div>
        </div>
        </div>`
    }

    function onTagifyFixQuery(input) {
        input.value = search_parsed;
        return true;
    }


}