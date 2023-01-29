function ownKeys(object, enumerableOnly) { var keys = Object.keys(object); if (Object.getOwnPropertySymbols) { var symbols = Object.getOwnPropertySymbols(object); enumerableOnly && (symbols = symbols.filter(function (sym) { return Object.getOwnPropertyDescriptor(object, sym).enumerable; })), keys.push.apply(keys, symbols); } return keys; }
function _objectSpread(target) { for (var i = 1; i < arguments.length; i++) { var source = null != arguments[i] ? arguments[i] : {}; i % 2 ? ownKeys(Object(source), !0).forEach(function (key) { _defineProperty(target, key, source[key]); }) : Object.getOwnPropertyDescriptors ? Object.defineProperties(target, Object.getOwnPropertyDescriptors(source)) : ownKeys(Object(source)).forEach(function (key) { Object.defineProperty(target, key, Object.getOwnPropertyDescriptor(source, key)); }); } return target; }
function _defineProperty(obj, key, value) { key = _toPropertyKey(key); if (key in obj) { Object.defineProperty(obj, key, { value: value, enumerable: true, configurable: true, writable: true }); } else { obj[key] = value; } return obj; }
function _toPropertyKey(arg) { var key = _toPrimitive(arg, "string"); return _typeof(key) === "symbol" ? key : String(key); }
function _toPrimitive(input, hint) { if (_typeof(input) !== "object" || input === null) return input; var prim = input[Symbol.toPrimitive]; if (prim !== undefined) { var res = prim.call(input, hint || "default"); if (_typeof(res) !== "object") return res; throw new TypeError("@@toPrimitive must return a primitive value."); } return (hint === "string" ? String : Number)(input); }
function _typeof(obj) { "@babel/helpers - typeof"; return _typeof = "function" == typeof Symbol && "symbol" == typeof Symbol.iterator ? function (obj) { return typeof obj; } : function (obj) { return obj && "function" == typeof Symbol && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; }, _typeof(obj); }
/**
 * Around | Multipurpose Bootstrap HTML Template
 * Copyright 2022 Createx Studio
 * Theme core scripts
 * 
 * @author Createx Studio
 * @version 3.1.0
 */

(function () {
  'use strict';

  /**
   * Theme Mode Switch
   * Switch betwen light/dark mode. The chosen mode is saved to browser's local storage
  */
  var themeModeSwitch = function () {
    var modeSwitch = document.querySelector('[data-bs-toggle="mode"]');
    if (modeSwitch === null) return;
    var checkbox = modeSwitch.querySelector('.form-check-input');
    if (mode === 'dark') {
      root.classList.add('dark-mode');
      checkbox.checked = true;
    } else {
      root.classList.remove('dark-mode');
      checkbox.checked = false;
    }
    modeSwitch.addEventListener('click', function (e) {
      if (checkbox.checked) {
        root.classList.add('dark-mode');
        window.localStorage.setItem('mode', 'dark');
      } else {
        root.classList.remove('dark-mode');
        window.localStorage.setItem('mode', 'light');
      }
    });
  }();

  /**
   * Add solid background to fixed to top navigation bar
  */

  var stickyNavbar = function () {
    var navbar = document.querySelector('.navbar.fixed-top');
    if (navbar == null) return;
    var navbarClass = navbar.classList,
      scrollOffset = 20;
    var navbarStuck = function navbarStuck(e) {
      if (e.currentTarget.pageYOffset > scrollOffset) {
        navbar.classList.add('navbar-stuck');
        if (navbar.classList.contains('navbar-ignore-dark-mode')) {
          navbar.classList.remove('ignore-dark-mode');
        }
      } else {
        navbar.classList.remove('navbar-stuck');
        if (navbar.classList.contains('navbar-ignore-dark-mode')) {
          navbar.classList.add('ignore-dark-mode');
        }
      }
    };

    // On load
    window.addEventListener('load', function (e) {
      navbarStuck(e);
    });

    // On scroll
    window.addEventListener('scroll', function (e) {
      navbarStuck(e);
    });
  }();

  /**
   * Animation on scroll (AOS)
   * 
   * @requires https://github.com/michalsnik/aos
  */

  var animateOnscroll = function () {
    var animationToggle = document.querySelector('[data-aos]');
    if (animationToggle === null) return;
    AOS.init();
  }();

  /**
   * Anchor smooth scrolling
   * @requires https://github.com/cferdinandi/smooth-scroll/
  */

  var smoothScroll = function () {
    var selector = '[data-scroll]',
      fixedHeader = '[data-scroll-header]',
      scroll = new SmoothScroll(selector, {
        speed: 800,
        speedAsDuration: true,
        offset: function offset(anchor, toggle) {
          return toggle.dataset.scrollOffset || 20;
        },
        header: fixedHeader,
        updateURL: false
      });
  }();

  /**
   * Animate scroll to top button in/off view
  */

  var scrollTopButton = function () {
    var button = document.querySelector('.btn-scroll-top'),
      scrollOffset = 450;
    if (button == null) return;
    var offsetFromTop = parseInt(scrollOffset, 10),
      progress = button.querySelector('svg circle'),
      length = progress.getTotalLength();
    progress.style.strokeDasharray = length;
    progress.style.strokeDashoffset = length;
    var showProgress = function showProgress() {
      var scrollPercent = (document.body.scrollTop + document.documentElement.scrollTop) / (document.documentElement.scrollHeight - document.documentElement.clientHeight),
        draw = length * scrollPercent;
      progress.style.strokeDashoffset = length - draw;
    };
    window.addEventListener('scroll', function (e) {
      if (e.currentTarget.pageYOffset > offsetFromTop) {
        button.classList.add('show');
      } else {
        button.classList.remove('show');
      }
      showProgress();
    });
  }();

  /**
   * Cascading (Masonry) grid layout
   * 
   * @requires https://github.com/desandro/imagesloaded
   * @requires https://github.com/Vestride/Shuffle
  */

  var masonryGrid = function () {
    var grid = document.querySelectorAll('.masonry-grid'),
      masonry;
    if (grid === null) return;
    var _loop = function _loop(i) {
      masonry = new Shuffle(grid[i], {
        itemSelector: '.masonry-grid-item',
        sizer: '.masonry-grid-item'
      });
      imagesLoaded(grid[i]).on('progress', function () {
        masonry.layout();
      });

      // Filtering
      var filtersWrap = grid[i].closest('.masonry-filterable');
      if (filtersWrap === null) return {
        v: void 0
      };
      var filters = filtersWrap.querySelectorAll('.masonry-filters [data-group]');
      for (var n = 0; n < filters.length; n++) {
        filters[n].addEventListener('click', function (e) {
          var current = filtersWrap.querySelector('.masonry-filters .active'),
            target = this.dataset.group;
          if (current !== null) {
            current.classList.remove('active');
          }
          this.classList.add('active');
          masonry.filter(target);
          e.preventDefault();
        });
      }
    };
    for (var i = 0; i < grid.length; i++) {
      var _ret = _loop(i);
      if (_typeof(_ret) === "object") return _ret.v;
    }
  }();

  /**
   * Toggling password visibility in password input
  */

  var passwordVisibilityToggle = function () {
    var elements = document.querySelectorAll('.password-toggle');
    var _loop2 = function _loop2(i) {
      var passInput = elements[i].querySelector('.form-control'),
        passToggle = elements[i].querySelector('.password-toggle-btn');
      passToggle.addEventListener('click', function (e) {
        if (e.target.type !== 'checkbox') return;
        if (e.target.checked) {
          passInput.type = 'text';
        } else {
          passInput.type = 'password';
        }
      }, false);
    };
    for (var i = 0; i < elements.length; i++) {
      _loop2(i);
    }
  }();

  /**
   * Interactive map
   * @requires https://github.com/Leaflet/Leaflet
  */

  var interactiveMap = function () {
    var mapList = document.querySelectorAll('.interactive-map');
    if (mapList.length === 0) return;
    for (var i = 0; i < mapList.length; i++) {
      var mapOptions = mapList[i].dataset.mapOptions,
        mapOptionsExternal = mapList[i].dataset.mapOptionsJson,
        map = void 0;

      // Map options: Inline JSON data
      if (mapOptions && mapOptions !== '') {
        var mapOptionsObj = JSON.parse(mapOptions),
          mapLayer = mapOptionsObj.mapLayer || 'https://api.maptiler.com/maps/pastel/{z}/{x}/{y}.png?key=BO4zZpr0fIIoydRTOLSx',
          mapCenter = mapOptionsObj.center ? mapOptionsObj.center : [0, 0],
          mapZoom = mapOptionsObj.zoom || 1,
          scrollWheelZoom = mapOptionsObj.scrollWheelZoom === false ? false : true,
          markers = mapOptionsObj.markers;

        // Map setup
        map = L.map(mapList[i], {
          scrollWheelZoom: scrollWheelZoom
        }).setView(mapCenter, mapZoom);

        // Tile layer
        L.tileLayer(mapLayer, {
          tileSize: 512,
          zoomOffset: -1,
          minZoom: 1,
          attribution: "<a href=\"https://www.maptiler.com/copyright/\" target=\"_blank\">&copy; MapTiler</a> <a href=\"https://www.openstreetmap.org/copyright\" target=\"_blank\">&copy; OpenStreetMap contributors</a>",
          crossOrigin: true
        }).addTo(map);

        // Markers
        if (markers) {
          for (var n = 0; n < markers.length; n++) {
            var iconUrl = markers[n].iconUrl,
              shadowUrl = markers[n].shadowUrl,
              markerIcon = L.icon({
                iconUrl: iconUrl || 'assets/img/map/marker-icon.png',
                iconSize: [30, 43],
                iconAnchor: [14, 43],
                shadowUrl: shadowUrl || 'assets/img/map/marker-shadow.png',
                shadowSize: [41, 41],
                shadowAnchor: [13, 41],
                popupAnchor: [1, -40]
              }),
              popup = markers[n].popup;
            var marker = L.marker(markers[n].position, {
              icon: markerIcon
            }).addTo(map);
            if (popup) {
              marker.bindPopup(popup);
            }
          }
        }

        // Map option: No options provided
      } else {
        map = L.map(mapList[i]).setView([0, 0], 1);
        L.tileLayer('https://api.maptiler.com/maps/pastel/{z}/{x}/{y}.png?key=BO4zZpr0fIIoydRTOLSx', {
          tileSize: 512,
          zoomOffset: -1,
          minZoom: 1,
          attribution: "<a href=\"https://www.maptiler.com/copyright/\" target=\"_blank\">&copy; MapTiler</a> <a href=\"https://www.openstreetmap.org/copyright\" target=\"_blank\">&copy; OpenStreetMap contributors</a>",
          crossOrigin: true
        }).addTo(map);
      }
    }
  }();

  /**
   * Mouse move parallax effect
   * @requires https://github.com/wagerfield/parallax
  */

  var parallax = function () {
    var element = document.querySelectorAll('.parallax');
    for (var i = 0; i < element.length; i++) {
      var parallaxInstance = new Parallax(element[i]);
    }
  }();

  /**
   * Content carousel with extensive options to control behaviour and appearance
   * @requires https://github.com/nolimits4web/swiper
  */

  var carousel = function () {
    // forEach function
    var forEach = function forEach(array, callback, scope) {
      for (var i = 0; i < array.length; i++) {
        callback.call(scope, i, array[i]); // passes back stuff we need
      }
    };

    // Carousel initialisation
    var carousels = document.querySelectorAll('.swiper');
    forEach(carousels, function (index, value) {
      var options;
      if (value.dataset.swiperOptions != undefined) options = JSON.parse(value.dataset.swiperOptions);

      // Thumbnails
      if (options.thumbnails) {
        var images = options.thumbnails.images;
        options = Object.assign({}, options, {
          pagination: {
            el: options.thumbnails.el,
            clickable: true,
            bulletActiveClass: 'active',
            renderBullet: function renderBullet(index, className) {
              return "<li class='swiper-thumbnail ".concat(className, "'>\n              <img src='").concat(images[index], "' alt='Thumbnail'>\n            </li>");
            }
          }
        });
      }
      var swiper = new Swiper(value, options);

      // Controlled slider
      if (options.controlledSlider) {
        var controlledSlider = document.querySelector(options.controlledSlider),
          controlledSliderOptions;
        if (controlledSlider.dataset.swiperOptions != undefined) controlledSliderOptions = JSON.parse(controlledSlider.dataset.swiperOptions);
        var swiperControlled = new Swiper(controlledSlider, controlledSliderOptions);
        swiper.controller.control = swiperControlled;
      }

      // Binded content
      if (options.bindedContent) {
        swiper.on('activeIndexChange', function (e) {
          var targetItem = document.querySelector(e.slides[e.activeIndex].dataset.swiperBinded),
            previousItem = document.querySelector(e.slides[e.previousIndex].dataset.swiperBinded);
          previousItem.classList.remove('active');
          targetItem.classList.add('active');
        });
      }
    });
  }();

  /**
   * Gallery like styled lightbox component for presenting various types of media
   * @requires https://github.com/sachinchoolur/lightGallery
  */

  var gallery = function () {
    var gallery = document.querySelectorAll('.gallery');
    if (gallery.length) {
      for (var i = 0; i < gallery.length; i++) {
        var thumbnails = gallery[i].dataset.thumbnails ? true : false,
          video = gallery[i].dataset.video ? true : false,
          defaultPlugins = [lgZoom, lgFullscreen],
          videoPlugin = video ? [lgVideo] : [],
          thumbnailPlugin = thumbnails ? [lgThumbnail] : [],
          plugins = [].concat(defaultPlugins, videoPlugin, thumbnailPlugin);
        lightGallery(gallery[i], {
          selector: '.gallery-item',
          plugins: plugins,
          licenseKey: 'D4194FDD-48924833-A54AECA3-D6F8E646',
          download: false,
          autoplayVideoOnSlide: true,
          zoomFromOrigin: false,
          youtubePlayerParams: {
            modestbranding: 1,
            showinfo: 0,
            rel: 0
          },
          vimeoPlayerParams: {
            byline: 0,
            portrait: 0,
            color: '6366f1'
          }
        });
      }
    }
  }();

  /**
   * Charts
   * @requires https://github.com/gionkunz/chartist-js
  */

  var charts = function () {
    var chart = document.querySelectorAll('[data-chart]');
    if (chart.length === 0) return;

    // Line chart
    for (var i = 0; i < chart.length; i++) {
      var dataOptions = JSON.parse(chart[i].dataset.chart);
      new Chart(chart[i], dataOptions);
    }
  }();

  /**
   * Range slider
   * @requires https://github.com/leongersen/noUiSlider
  */

  var rangeSlider = function () {
    var rangeSliderWidget = document.querySelectorAll('.range-slider');
    var _loop3 = function _loop3(i) {
      var rangeSlider = rangeSliderWidget[i].querySelector('.range-slider-ui'),
        valueMinInput = rangeSliderWidget[i].querySelector('.range-slider-value-min'),
        valueMaxInput = rangeSliderWidget[i].querySelector('.range-slider-value-max');
      var options = {
        dataStartMin: parseInt(rangeSliderWidget[i].dataset.startMin, 10),
        dataStartMax: parseInt(rangeSliderWidget[i].dataset.startMax, 10),
        dataMin: parseInt(rangeSliderWidget[i].dataset.min, 10),
        dataMax: parseInt(rangeSliderWidget[i].dataset.max, 10),
        dataStep: parseInt(rangeSliderWidget[i].dataset.step, 10),
        dataPips: rangeSliderWidget[i].dataset.pips
      };
      var start = options.dataStartMax ? [options.dataStartMin, options.dataStartMax] : [options.dataStartMin],
        connect = options.dataStartMax ? true : 'lower';
      noUiSlider.create(rangeSlider, {
        start: start,
        connect: connect,
        step: options.dataStep,
        pips: options.dataPips ? {
          mode: 'count',
          values: 5
        } : false,
        tooltips: true,
        range: {
          'min': options.dataMin,
          'max': options.dataMax
        },
        format: {
          to: function to(value) {
            return '$' + parseInt(value, 10);
          },
          from: function from(value) {
            return Number(value);
          }
        }
      });
      rangeSlider.noUiSlider.on('update', function (values, handle) {
        var value = values[handle];
        value = value.replace(/\D/g, '');
        if (handle) {
          if (valueMaxInput) {
            valueMaxInput.value = Math.round(value);
          }
        } else {
          if (valueMinInput) {
            valueMinInput.value = Math.round(value);
          }
        }
      });
      if (valueMinInput) {
        valueMinInput.addEventListener('change', function () {
          rangeSlider.noUiSlider.set([this.value, null]);
        });
      }
      if (valueMaxInput) {
        valueMaxInput.addEventListener('change', function () {
          rangeSlider.noUiSlider.set([null, this.value]);
        });
      }
    };
    for (var i = 0; i < rangeSliderWidget.length; i++) {
      _loop3(i);
    }
  }();

  /**
   * Date / time picker
   * @requires https://github.com/flatpickr/flatpickr
   */

  var datePicker = function () {
    var picker = document.querySelectorAll('.date-picker');
    if (picker.length === 0) return;
    for (var i = 0; i < picker.length; i++) {
      var defaults = {
        disableMobile: 'true'
      };
      var userOptions = void 0;
      if (picker[i].dataset.datepickerOptions != undefined) userOptions = JSON.parse(picker[i].dataset.datepickerOptions);
      var linkedInput = picker[i].classList.contains('date-range') ? {
        'plugins': [new rangePlugin({
          input: picker[i].dataset.linkedInput
        })]
      } : '{}';
      var options = _objectSpread(_objectSpread(_objectSpread({}, defaults), linkedInput), userOptions);
      flatpickr(picker[i], options);
    }
  }();

  /**
   * FullCalendar plugin initialization (Schedule)
   * @requires https://github.com/fullcalendar/fullcalendar
  */

  var calendar = function () {
    // forEach function
    var forEach = function forEach(array, callback, scope) {
      for (var i = 0; i < array.length; i++) {
        callback.call(scope, i, array[i]); // passes back stuff we need
      }
    };

    // Calendar initialisation
    var calendars = document.querySelectorAll('.calendar');
    forEach(calendars, function (index, value) {
      var userOptions;
      if (value.dataset.calendarOptions != undefined) userOptions = JSON.parse(value.dataset.calendarOptions);
      var options = _objectSpread({
        themeSystem: 'bootstrap5'
      }, userOptions);
      var calendarInstance = new FullCalendar.Calendar(value, options);
      calendarInstance.render();
    });
  }();

  /**
   * Form validation
  */

  var formValidation = function () {
    var selector = 'needs-validation';
    window.addEventListener('load', function () {
      // Fetch all the forms we want to apply custom Bootstrap validation styles to
      var forms = document.getElementsByClassName(selector);
      // Loop over them and prevent submission
      var validation = Array.prototype.filter.call(forms, function (form) {
        form.addEventListener('submit', function (e) {
          if (form.checkValidity() === false) {
            e.preventDefault();
            e.stopPropagation();
          }
          form.classList.add('was-validated');
        }, false);
      });
    }, false);
  }();

  /**
   * Input fields formatter
   * @requires https://github.com/nosir/cleave.js
  */

  var inputFormatter = function () {
    var input = document.querySelectorAll('[data-format]');
    if (input.length === 0) return;
    var _loop4 = function _loop4(i) {
      var targetInput = input[i],
        cardIcon = targetInput.parentNode.querySelector('.credit-card-icon'),
        options = void 0,
        formatter = void 0;
      if (targetInput.dataset.format != undefined) options = JSON.parse(targetInput.dataset.format);
      if (cardIcon) {
        formatter = new Cleave(targetInput, _objectSpread(_objectSpread({}, options), {}, {
          onCreditCardTypeChanged: function onCreditCardTypeChanged(type) {
            cardIcon.className = 'credit-card-icon ' + type;
          }
        }));
      } else {
        formatter = new Cleave(targetInput, options);
      }
    };
    for (var i = 0; i < input.length; i++) {
      _loop4(i);
    }
  }();

  /**
   * Update the text of the label when radio button / checkbox changes
  */

  var bindedLabel = function () {
    var toggleBtns = document.querySelectorAll('[data-binded-label]');
    for (var i = 0; i < toggleBtns.length; i++) {
      toggleBtns[i].addEventListener('change', function () {
        var target = this.dataset.bindedLabel;
        try {
          document.getElementById(target).textContent = this.value;
        } catch (err) {
          if (err.message = "Cannot set property 'textContent' of null") {
            console.error('Make sure the [data-binded-label] matches with the id of the target element you want to change text of!');
          }
        }
      });
    }
  }();

  /**
   * Bind different content to different navs or even accordion.
  */

  var bindedContent = function () {
    var clickToggles = document.querySelectorAll('[data-binded-content]'),
      scrollToggles = document.querySelectorAll('[data-scroll-binded]'),
      bindedContent = document.querySelector('.binded-content');

    // Get target element siblings
    var getSiblings = function getSiblings(elem) {
      var siblings = [],
        sibling = elem.parentNode.firstChild;
      while (sibling) {
        if (sibling.nodeType === 1 && sibling !== elem) {
          siblings.push(sibling);
        }
        sibling = sibling.nextSibling;
      }
      return siblings;
    };

    // Change binded content function
    var changeBindedContent = function changeBindedContent(target) {
      var targetEl = document.querySelector(target),
        targetSiblings = getSiblings(targetEl);
      targetSiblings.map(function (sibling) {
        sibling.classList.remove('active');
      });
      targetEl.classList.add('active');
    };

    // Change binded content on click
    for (var i = 0; i < clickToggles.length; i++) {
      clickToggles[i].addEventListener('click', function (e) {
        changeBindedContent(e.currentTarget.dataset.bindedContent);
      });
    }
  }();

  /**
   * Count input with increment (+) and decrement (-) buttons
  */

  var countInput = function () {
    var countInputs = document.querySelectorAll('.count-input');
    var _loop5 = function _loop5(i) {
      var component = countInputs[i],
        incrementBtn = component.querySelector('[data-increment]'),
        decrementBtn = component.querySelector('[data-decrement]'),
        input = component.querySelector('.form-control');
      var handleIncrement = function handleIncrement() {
        input.value++;
      };
      var handleDecrement = function handleDecrement() {
        if (input.value > 0) {
          input.value--;
        }
      };

      // Add click event to buttons
      incrementBtn.addEventListener('click', handleIncrement);
      decrementBtn.addEventListener('click', handleDecrement);
    };
    for (var i = 0; i < countInputs.length; i++) {
      _loop5(i);
    }
  }();

  /**
   * Focus first input on modal / offcanvas / collapse open
   * 
  */

  var inputAutoFocus = function () {
    var targetInput = document.querySelectorAll('[data-focus-on-open]');
    if (targetInput === null) return;
    var _loop6 = function _loop6(i) {
      var toggler = JSON.parse(targetInput[i].dataset.focusOnOpen);
      document.querySelector(toggler[1]).addEventListener("shown.bs.".concat(toggler[0]), function (e) {
        targetInput[i].focus();
      });
    };
    for (var i = 0; i < targetInput.length; i++) {
      _loop6(i);
    }
  }();

  /**
   * Tooltip
   * @requires https://getbootstrap.com
   * @requires https://popper.js.org/
  */

  var tooltip = function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
      return new bootstrap.Tooltip(tooltipTriggerEl, {
        trigger: 'hover'
      });
    });
  }();

  /**
   * Popover
   * @requires https://getbootstrap.com
   * @requires https://popper.js.org/
  */

  var popover = function () {
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
      return new bootstrap.Popover(popoverTriggerEl);
    });
  }();

  /**
   * Toast
   * @requires https://getbootstrap.com
  */

  var toast = function () {
    var toastElList = [].slice.call(document.querySelectorAll('.toast'));
    var toastList = toastElList.map(function (toastEl) {
      return new bootstrap.Toast(toastEl);
    });
  }();

  /**
   * Open YouTube video in lightbox
   * @requires https://github.com/sachinchoolur/lightGallery
  */

  var videoButton = function () {
    var button = document.querySelectorAll('[data-bs-toggle="video"]');
    if (button.length) {
      for (var i = 0; i < button.length; i++) {
        lightGallery(button[i], {
          selector: 'this',
          plugins: [lgVideo],
          licenseKey: 'D4194FDD-48924833-A54AECA3-D6F8E646',
          download: false,
          youtubePlayerParams: {
            modestbranding: 1,
            showinfo: 0,
            rel: 0
          },
          vimeoPlayerParams: {
            byline: 0,
            portrait: 0,
            color: '6366f1'
          }
        });
      }
    }
  }();

  /**
   * Price switch
  */

  var priceSwitch = function () {
    var switchWrapper = document.querySelectorAll('.price-switch-wrapper');
    if (switchWrapper.length <= 0) return;
    var showMonthlyPrice = function showMonthlyPrice(monthlyPrice, annualPrice) {
      for (var n = 0; n < monthlyPrice.length; n++) {
        annualPrice[n].classList.add('d-none');
        monthlyPrice[n].classList.remove('d-none');
      }
    };
    var showAnnualPrice = function showAnnualPrice(monthlyPrice, annualPrice) {
      for (var n = 0; n < monthlyPrice.length; n++) {
        monthlyPrice[n].classList.add('d-none');
        annualPrice[n].classList.remove('d-none');
      }
    };
    for (var i = 0; i < switchWrapper.length; i++) {
      var switchToggle = switchWrapper[i].querySelector('[data-bs-toggle="price"]');
      switchToggle.addEventListener('change', function (e) {
        var monthlySwitch = e.currentTarget.querySelector('[data-monthly-switch]'),
          annualSwitch = e.currentTarget.querySelector('[data-annual-switch]'),
          monthlyPrice = e.currentTarget.closest('.price-switch-wrapper').querySelectorAll('[data-monthly-price]'),
          annualPrice = e.currentTarget.closest('.price-switch-wrapper').querySelectorAll('[data-annual-price]');
        if (monthlySwitch.checked == true) showMonthlyPrice(monthlyPrice, annualPrice);
        if (annualSwitch.checked == true) showAnnualPrice(monthlyPrice, annualPrice);
      });
    }
  }();

  /**
   * Toggle that checkes / unchecks all target checkboxes at once
  */

  var checkboxToggle = function () {
    var toggler = document.querySelectorAll('[data-bs-toggle="checkbox"]');
    if (toggler.length === 0) return;
    for (var i = 0; i < toggler.length; i++) {
      toggler[i].addEventListener('click', function (e) {
        e.preventDefault();
        var checkboxListContainer = document.querySelector(e.target.dataset.bsTarget),
          checkboxList = checkboxListContainer.querySelectorAll('input[type="checkbox"]');
        checkboxListContainer.classList.toggle('all-checked');
        if (checkboxListContainer.classList.contains('all-checked')) {
          for (var n = 0; n < checkboxList.length; n++) {
            checkboxList[n].checked = true;
          }
        } else {
          for (var m = 0; m < checkboxList.length; m++) {
            checkboxList[m].checked = false;
          }
        }
      });
    }
  }();

  /**
   * Countdown timer
   * @requires https://github.com/BrooonS/timezz
  */

  var countdown = function () {
    var timers = document.querySelectorAll('.countdown');
    if (timers.length === 0) return;
    for (var i = 0; i < timers.length; i++) {
      var date = timers[i].dataset.countdownDate;
      timezz(timers[i], {
        date: date
        // add more options here
      });
    }
  }();

  /**
   * Ajaxify MailChimp subscription form
  */

  var subscriptionForm = function () {
    var form = document.querySelectorAll('.subscription-form');
    if (form === null) return;
    var _loop7 = function _loop7(i) {
      var button = form[i].querySelector('button[type="submit"]'),
        buttonText = button.innerHTML,
        input = form[i].querySelector('.form-control'),
        antispam = form[i].querySelector('.subscription-form-antispam'),
        status = form[i].querySelector('.subscription-status');
      form[i].addEventListener('submit', function (e) {
        if (e) e.preventDefault();
        if (antispam.value !== '') return;
        register(this, button, input, buttonText, status);
      });
    };
    for (var i = 0; i < form.length; i++) {
      _loop7(i);
    }
    var register = function register(form, button, input, buttonText, status) {
      button.innerHTML = 'Sending...';

      // Get url for MailChimp
      var url = form.action.replace('/post?', '/post-json?');

      // Add form data to object
      var data = '&' + input.name + '=' + encodeURIComponent(input.value);

      // Create and add post script to the DOM
      var script = document.createElement('script');
      script.src = url + '&c=callback' + data;
      document.body.appendChild(script);

      // Callback function
      var callback = 'callback';
      window[callback] = function (response) {
        // Remove post script from the DOM
        delete window[callback];
        document.body.removeChild(script);

        // Change button text back to initial
        button.innerHTML = buttonText;

        // Display content and apply styling to response message conditionally
        if (response.result == 'success') {
          input.classList.remove('is-invalid');
          input.classList.add('is-valid');
          status.classList.remove('status-error');
          status.classList.add('status-success');
          status.innerHTML = response.msg;
          setTimeout(function () {
            input.classList.remove('is-valid');
            status.innerHTML = '';
            status.classList.remove('status-success');
          }, 6000);
        } else {
          input.classList.remove('is-valid');
          input.classList.add('is-invalid');
          status.classList.remove('status-success');
          status.classList.add('status-error');
          status.innerHTML = response.msg.substring(4);
          setTimeout(function () {
            input.classList.remove('is-invalid');
            status.innerHTML = '';
            status.classList.remove('status-error');
          }, 6000);
        }
      };
    };
  }();

  /**
   * Around | Multipurpose Bootstrap HTML Template
   * Copyright 2022 Createx Studio
   * Theme core scripts
   *
   * @author Createx Studio
   * @version 3.0.0
  */
})();