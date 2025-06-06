﻿(function (funcName, baseObj) {
    "use strict";
    // The public function name defaults to window.docReady
    // but you can modify the last line of this function to pass in a different object or method name
    // if you want to put them in a different namespace and those will be used instead of 
    // window.docReady(...)
    funcName = funcName || "docReady";
    baseObj = baseObj || window;
    var readyList = [];
    var readyFired = false;
    var readyEventHandlersInstalled = false;

    // call this when the document is ready
    // this function protects itself against being called more than once
    function ready() {
        if (!readyFired) {
            // this must be set to true before we start calling callbacks
            readyFired = true;
            for (var i = 0; i < readyList.length; i++) {
                // if a callback here happens to add new ready handlers,
                // the docReady() function will see that it already fired
                // and will schedule the callback to run right after
                // this event loop finishes so all handlers will still execute
                // in order and no new ones will be added to the readyList
                // while we are processing the list
                readyList[i].fn.call(window, readyList[i].ctx);
            }
            // allow any closures held by these functions to free
            readyList = [];
        }
    }

    function readyStateChange() {
        if (document.readyState === "complete") {
            ready();
        }
    }

    // This is the one public interface
    // docReady(fn, context);
    // the context argument is optional - if present, it will be passed
    // as an argument to the callback
    baseObj[funcName] = function (callback, context) {
        if (typeof callback !== "function") {
            throw new TypeError("callback for docReady(fn) must be a function");
        }
        // if ready has already fired, then just schedule the callback
        // to fire asynchronously, but right away
        if (readyFired) {
            setTimeout(function () { callback(context); }, 1);
            return;
        } else {
            // add the function and context to the list
            readyList.push({ fn: callback, ctx: context });
        }
        // if document already ready to go, schedule the ready function to run
        // IE only safe when readyState is "complete", others safe when readyState is "interactive"
        if (document.readyState === "complete" || (!document.attachEvent && document.readyState === "interactive")) {
            setTimeout(ready, 1);
        } else if (!readyEventHandlersInstalled) {
            // otherwise if we don't have event handlers installed, install them
            if (document.addEventListener) {
                // first choice is DOMContentLoaded event
                document.addEventListener("DOMContentLoaded", ready, false);
                // backup is window load event
                window.addEventListener("load", ready, false);
            } else {
                // must be IE
                document.attachEvent("onreadystatechange", readyStateChange);
                window.attachEvent("onload", ready);
            }
            readyEventHandlersInstalled = true;
        }
    }
})("docReady", window);
// modify this previous line to pass in your own method name 
// and object for the method to be attached to


(function () {

    /* Load iFrameResize  if not present */
    //console.log("testing iFrameResize");
    if (!(typeof iFrameResize === 'function')) {
        //console.log("testing iFrameResize done - missing");
        var script_tag = document.createElement('script');
        script_tag.setAttribute("type", "text/javascript");
        script_tag.setAttribute("src",
            "#WEBROOT#/scripts/iframeResizer.min.js");
        if (script_tag.readyState) {
            script_tag.onreadystatechange = function () { // For old versions of IE
                if (this.readyState == 'complete' || this.readyState == 'loaded') {
                    scriptLoadHandler();
                }
            };
        }
        else {
            //console.log("testing iFrameResize done - is here");
            script_tag.onload = scriptLoadHandler;
        }

        (document.getElementsByTagName("head")[0] || document.documentElement).appendChild(script_tag);
    }
    else {
        // The jQuery version on the window is the one we want to use
        main();
    }


    /* Called once jQuery has loaded */
    function scriptLoadHandler() {
        main();
    }

    /* Our Start function */
    function main() {
        docReady(function () {
            /* Get 'embed' parameter from the query */
            var targetElem = document.getElementById('#RND#');
            var origPage = '#WEBROOT#';
            if (targetElem.getAttribute("widget-page"))
            {
                origPage = origPage + targetElem.getAttribute("widget-page");
                //console.log("page " + targetElem.getAttribute("widget-page"));
            }
            var widget = origPage;
            if (widget.indexOf("embed=1") !== -1)
                return;

            if (widget.indexOf("?") !== -1)
                widget = widget + '&embed=1';
            else
                widget = widget + '?embed=1&maxwidth=#MAXWIDTH#';
            var caller = encodeURIComponent(window.document.location);
            var iframeContent = ''
                + '<div style="text-align: right !important;font-size:80%"><a href="' + origPage + '" target="_top">Ukázat celou stránku</a>. (c) Hlídač Státu</div>'
                + '<iframe onload="iFrameResize({ log: false #MAXWIDTHSCRIPT# });" style="border: 0;width:100%" frameborder="0" width="100%" border="0" cellspacing="0" src="' + widget
                + '&calledFrom=' + caller + '"></iframe>'
                //+ '<div style="text-align: right !important;font-size:80%"><a href="' + origPage + '" target="_top">Ukázat celou stránku</a> z HlidacStatu.cz</div>'
            ;

            targetElem.innerHTML = iframeContent;
        });
    }

})();