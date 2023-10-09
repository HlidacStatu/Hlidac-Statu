(function ($) {

	Date.DEFAULT_LOCALE = 'en-US';

	Date.UNITS = {
		year: 24 * 60 * 60 * 1000 * 365,
		month: 24 * 60 * 60 * 1000 * 365 / 12,
		week: 24 * 60 * 60 * 1000 * 7,
		day: 24 * 60 * 60 * 1000,
		hour: 60 * 60 * 1000,
		minute: 60 * 1000,
		second: 1000
	}

	/**
	 *
	 * @param {string} locale
	 */
	Date.setLocale = function (locale) {
		Date.DEFAULT_LOCALE = locale ? locale : Date.DEFAULT_LOCALE;
	};

	Date.getUnits = function () {
		return Date.UNITS;
	}


	/**
	 *
	 * @param {boolean} abbreviation
	 * @returns {string[]}
	 */
	Date.getDayNames = function (abbreviation = false) {
		const formatter = new Intl.DateTimeFormat(Date.DEFAULT_LOCALE, {weekday: abbreviation ? 'short' : 'long', timeZone: 'UTC'});
		const days = [2, 3, 4, 5, 6, 7, 8].map(day => {
			const dd = day < 10 ? `0${day}` : day;
			return new Date(`2017-01-${dd}T00:00:00+00:00`);
		});
		return days.map(date => formatter.format(date));
	}

	/**
	 *
	 * @param {boolean} abbreviation
	 * @returns {string[]}
	 */
	Date.getMonthNames = function (abbreviation = false) {
		const formatter = new Intl.DateTimeFormat(Date.DEFAULT_LOCALE, {month: abbreviation ? 'short' : 'long', timeZone: 'UTC'});
		const months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(month => {
			const mm = month < 10 ? `0${month}` : month;
			return new Date(`2017-${mm}-01T00:00:00+00:00`);
		});
		return months.map(date => formatter.format(date));
	}
	/**
	 *
	 * @param {boolean} abbreviation
	 * @returns {string}
	 */
	Date.prototype.showDateFormatted = function () {
		let options = {weekday: 'long', year: 'numeric', month: 'short', day: 'numeric'};
		return this.toLocaleDateString(Date.DEFAULT_LOCALE, options);

	}

	/**
	 *
	 * @returns {Date}
	 */
	Date.prototype.copy = function () {
		return this.clone();
	}


	/**
	 *
	 * @param {boolean} abbreviation
	 * @return {string}
	 */
	Date.prototype.getMonthName = function (abbreviation = false) {
		const formatter = new Intl.DateTimeFormat(Date.DEFAULT_LOCALE, {month: abbreviation ? 'short' : 'long', timeZone: 'UTC'});
		return formatter.format(this);
	};

	/**
	 * Determine the number of days in the current month
	 * @returns {number}
	 */
	Date.prototype.getDaysInMonth = function () {
		return [31, (this.isLeapYear() ? 29 : 28), 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][this.getMonth()];
	};

	/**
	 * Checks if the year of the date is a leap year
	 * @returns {boolean}
	 */
	Date.prototype.isLeapYear = function () {
		let year = this.getFullYear();
		return (((year % 4 === 0) && (year % 100 !== 0)) || (year % 400 === 0));
	};

	/**
	 *
	 * @param {boolean} abbreviation
	 * @return {string}
	 */
	Date.prototype.getDayName = function (abbreviation = false) {
		const formatter = new Intl.DateTimeFormat(Date.DEFAULT_LOCALE, {weekday: abbreviation ? 'short' : 'long', timeZone: 'UTC'});
		return formatter.format(this);
	};

	$.fn.extend({
		triggerAll: function (events, params) {
			let el = this, i, evts = events.split(' ');
			for (i = 0; i < evts.length; i += 1) {
				el.trigger(evts[i], params);
			}
			return el;
		}
	});

	$.bsCalendar = {
		setDefault: function (prop, value) {
			this.DEFAULTS[prop] = value;
		},
		getDefaults: function (container) {
			this.DEFAULTS.url = container.data('target') || container.data('bsTarget') || this.DEFAULTS.url;
			return this.DEFAULTS;
		},
		DEFAULTS: {
			locale: 'en',
			url: null,
			width: '300px',
			icons: {
				prev: 'fa-solid fa-arrow-left fa-fw',
				next: 'fa-solid fa-arrow-right fa-fw',
				eventEdit: 'fa-solid fa-edit fa-fw',
				eventRemove: 'fa-solid fa-trash fa-fw'
			},
			showEventEditButton: false,
			showEventRemoveButton: false,
			formatEvent: function (event) {
				return drawEvent(event);
			},
			formatNoEvent: function (date) {
				return drawNoEvent(date);
			},
			queryParams: function (params) {
				return params;
			},
			onClickEditEvent: function (e, event) {
			},
			onClickDeleteEvent: function (e, event) {
			},
		}
	};

	let eventEditButton = {
		class: 'btn btn-link p-0 .js-edit-event',
		icon: '',
	};

	let eventRemoveButton = {
		class: 'btn btn-link p-0 .js-delete-event',
		icon: 'fa-solid fa-trash fa-fw'
	};

	/**
	 *
	 * @param days
	 * @returns {Date}
	 */
	Date.prototype.addDays = function (days) {
		let date = new Date(this.valueOf());
		date.setDate(date.getDate() + days);
		return date;
	}

	/**
	 *
	 * @param days
	 * @returns {Date}
	 */
	Date.prototype.subDays = function (days) {
		let date = new Date(this.valueOf());
		date.setDate(date.getDate() - days);
		return date;
	}

	/**
	 * Subtracts a specified number of months from the date
	 * @param {number} months
	 * @returns {Date}
	 */
	Date.prototype.subMonths = function (months) {
		const month = this.getMonth();
		this.setMonth(this.getMonth() - months);
		while (this.getMonth() === month) {
			this.subDays(1);
		}
		return this;
	}
	/**
	 *
	 * @param year
	 * @returns {boolean}
	 */
	Date.isLeapYear = function (year) {
		return (((year % 4 === 0) && (year % 100 !== 0)) || (year % 400 === 0));
	};


	/**
	 * Adds a specified number of months to the date
	 * @param {number} months
	 * @returns {Date}
	 */
	Date.prototype.addMonths = function (months) {
		let n = this.getDate();
		this.setDate(1);
		this.setMonth(this.getMonth() + months);
		this.setDate(Math.min(n, this.getDaysInMonth()));
		return this;
	};

	/**
	 * Determine the first day of the current month
	 * @returns {Date}
	 */
	Date.prototype.getFirstDayOfMonth = function () {
		return new Date(this.getFullYear(), this.getMonth(), 1);
	}


	/**
	 * Determine the last day of the current month
	 * @returns {Date}
	 */
	Date.prototype.getLastDayOfMonth = function () {
		return new Date(this.getFullYear(), this.getMonth() + 1, 0);
	}

	/**
	 *
	 * @returns {Date}
	 */
	Date.prototype.getMonday = function () {
		let d = new Date(this.valueOf());
		let day = d.getDay(),
			diff = d.getDate() - day + (day === 0 ? -6 : 1); // adjust when day is sunday
		return new Date(d.setDate(diff));
	}


	/**
	 * Determine the previous Monday of the current date
	 * @returns {Date}
	 */
	Date.prototype.getFirstDayOfWeek = function () {
		let d = this.copy();
		let day = d.getDay(),
			diff = d.getDate() - day + (day === 0 ? -6 : 1); // adjust when day is sunday
		return new Date(d.setDate(diff));
	}


	/**
	 * Determine the Sunday of the current date
	 * @returns {Date}
	 */
	Date.prototype.getLastDayOfWeek = function () {
		let d = this.clone();
		const first = d.getDate() - d.getDay() + 1;
		const last = first + 6;

		return new Date(d.setDate(last));
	}

	/**
	 *
	 * @returns {Date}
	 */
	Date.prototype.getSunday = function () {
		let d = new Date(this.valueOf());
		const first = d.getDate() - d.getDay() + 1;
		const last = first + 6;

		return new Date(d.setDate(last));
	}
	/**
	 *
	 * @returns {Date}
	 */
	Date.prototype.clone = function () {
		return new Date(this.valueOf());
	}

	/**
	 *
	 * @param asArray
	 * @returns {(number|string)[]|string}
	 */
	Date.prototype.formatDate = function (asArray) {
		let d = new Date(this.valueOf()),
			month = '' + (d.getMonth() + 1),
			day = '' + d.getDate(),
			year = d.getFullYear();

		if (month.length < 2)
			month = '0' + month;
		if (day.length < 2)
			day = '0' + day;

		return asArray ? [year, month, day] : [year, month, day].join('-');
	}

	function padTo2Digits(num) {
		return num.toString().padStart(2, '0');
	}

	function formatDateReadable(date, separator = '.') {
		return date.showDateFormatted();
		return date.getDayName() + ' ' + [

			padTo2Digits(date.getDate()),
			padTo2Digits(date.getMonth() + 1),
			date.getFullYear(),
		].join(separator);
	}

	/**
	 *
	 * @returns {number}
	 */
	Date.prototype.getWeek = function () {
		// Copy date so don't modify original
		let d = new Date(Date.UTC(this.getFullYear(), this.getMonth(), this.getDate()));
		// Set to the nearest Thursday: current date + 4 - current day number
		// Make Sunday's day number 7
		d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7));
		// Get first day of year
		let yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
		// Calculate full weeks to the nearest Thursday and return  week number
		return Math.ceil((((d - yearStart) / 86400000) + 1) / 7);
	}


	/**
	 *
	 * @param date
	 * @returns {string}
	 */
	function formatDate(date) {
		let date_arr = date.split('-');
		return date_arr[2] + '.' + date_arr[1] + '.' + date_arr[0];
	}

	/**
	 *
	 * @param time
	 * @returns {string}
	 */
	function formatTime(time) {
		return time.substring(0, 5);
	}

	/**
	 *
	 * @param {object} event
	 * @returns {string}
	 */
	function drawEvent(event) {
		let desc = event.description ? '<p class="m-0">' + event.description + '</p>' : '';
		let s, e;
		let startDate = event.start.split(' ')[0];
		let endDate = event.end.split(' ')[0];
		if (startDate === endDate) {
			s = formatTime(event.start.split(' ')[1]);
			e = formatTime(event.end.split(' ')[1]);
		} else {
			s = formatDate(startDate);
			e = formatDate(endDate);
		}
		return `
        <div class="d-flex flex-column" style="font-size:.8em">
            <h6 class="mb-0 text-uppercase">${event.title}</h6>
            <small class="text-muted">${s} - ${e}</small>
            ${desc}
        </div>
        `;
	}

	/**
	 * @param {Date} date
	 * @returns {string}
	 */
	function drawNoEvent(date) {
		return `
        <div class="p-2" style="font-size:.8em">
        <h6 class="mb-0 text-uppercase">Keine Termine</h6>
        </div>
        `;
	}

	/**
	 *
	 * @param {object} container
	 */
	function drawTemplate(container) {
		console.log(typeof container);
		let settings = container.data('settings');
		let today = new Date();
		container
			.css({'width': settings.width})
			.addClass('bg-white')
			.html(`
			<div class="p-3 d-flex flex-column text-muted">
				<small>${today.getDayName()}</small>
				<h4 class="mb-0">${today.getDate()}. ${today.getMonthName()} ${today.getFullYear()}</h4>
			</div>
            <div class="d-flex flex-nowrap justify-content-between align-items-center p-2">
                <a href="#" class="btn btn-light btn-prev-month"><i class="${settings.icons.prev}"></i></a>
                <a href="#" class="btn btn-light mx-1 flex-fill btn-curr-month month-name"></a>
                <a href="#" class="btn btn-light btn-next-month"><i class="${settings.icons.next}"></i></a>
            </div>
            <div class="d-flex flex-nowrap align-items-center js-weekdays bg-light">
                <div class="text-center bg-light"></div>
            </div>
             <div class="js-weeks"></div>
            <div class="dates"></div>
            <div class="pt-2 js-collapse" style="display: none">
                <div class="card mb-0">
                    <div class="text-center fw-bold py-2 js-day-name card-header"></div>
                    <div class="js-events list-group list-group-flush"></div>
                </div>
                
            </div>
        `);

		let cellWidthHeight = container.width() / 8; // 7 days + calendar week
		let fontSize = cellWidthHeight / 3;
		let cellCss = {
			color: '#adadad',
			lineHeight: cellWidthHeight + 'px',
			fontSize: fontSize + 'px',
			height: cellWidthHeight,
			width: cellWidthHeight,
		};
		container.find('.js-weekdays div:first').css(cellCss);


		Date.getDayNames(true).forEach(wd => {
			$('<div>', {
				html: wd,
				class: 'text-center bg-light',
				css: cellCss,
			}).appendTo(container.find('.js-weekdays'));
		});
	}

	/**
	 *
	 * @param {object|null} options
	 * @param {object|null|string|Date} options
	 * @returns {*|jQuery|HTMLElement}
	 */
	$.fn.bsCalendar = function (options, params) {

		const container = $(this);
		const today = new Date();
		let xhr = null;
		let current = new Date();

		let isOptionsSet = typeof options === 'object';
		let isMethodSet = typeof options === 'string';


		let settings;

		if (container.data('init') !== true) {

			settings = $.extend($.bsCalendar.getDefaults(container), options || {});
			Date.setLocale(settings.locale);
			container.data('settings', $.extend($.bsCalendar.getDefaults(container), options || {}));
			drawTemplate(container);
		} else {
			settings = container.data('settings');
		}


		let cellWidthHeight = container.width() / 8; // 7 days + calendar week
		let fontSize = cellWidthHeight / 3; // 7 days + calendar week


		// let containerCalendar = $('<div>').appendTo(container);
		let calendar = [];

		/**
		 *
		 * @param {object} container
		 * @param {Date} selected
		 * @param {function} callback
		 */
		function getEvents(container, selected, callback) {
			if (xhr) {
				xhr.abort();
				xhr = null;
			}

			if (!settings.url) {
				callback([]);
			} else {
				let data = {
					from: selected.clone().getFirstDayOfMonth().getFirstDayOfWeek().formatDate(false),
					to: selected.clone().getLastDayOfMonth().getLastDayOfWeek().formatDate(false)
				};
				xhr = $.ajax({
					url: settings.url,
					dataType: 'json',
					data: settings.queryParams(data),
					success: function (events) {
						container.trigger('events-loaded', [events]);
						callback(events || []);
					}
				});
			}
		}

		/**
		 *
		 * @param {Date} selectedDate
		 * @param {null|string} clickDate
		 */
		function drawCalendar(selectedDate, clickDate = null) {

			// if(clickDate !== null){
			// 	selectedDate = new Date(clickDate);
			// }

			let activeDate = container.find('[data-date].active').length ? container.find('[data-date].active').data('date') : null;

			let wrap = container.find('.js-weeks').empty();
			const startDay = selectedDate.clone().getFirstDayOfMonth().getMonday();
			const endDay = selectedDate.clone().getLastDayOfMonth().getSunday();

			let date = startDay.clone().subDays(1);

			container.find('.month-name').html(selectedDate.getMonthName() + ' ' + selectedDate.getFullYear());
			// container.find('.month-name').html(settings.months[selectedDate.getMonth()] + ' ' + selectedDate.getFullYear());

			calendar = [];
			while (date < endDay) {
				calendar.push({
					days: Array(7).fill(0).map(() => {
						date = date.addDays(1);
						return date;
					})
				});
			}

			let currentWeek = today.getWeek();
			let currentYear = today.getFullYear();

			calendar.forEach(week => {

				let w = week.days[0].getWeek();
				let highlight_week = currentYear === week.days[0].getFullYear() && currentWeek === w;
				let highlightClass = highlight_week ? 'fw-bold text-dark' : '';

				let weekContainer = $('<div>', {
					class: 'd-flex flex-nowrap'
				}).appendTo(wrap);

				// calendar week
				$('<div>', {
					class: 'd-flex justify-content-center align-items-center js-cal-row bg-light',
					css: {
						fontSize: fontSize + 'px',
						color: '#adadad',
						width: cellWidthHeight,
						height: cellWidthHeight
					},
					html: [
						'<small class="text-center mb-0 ' + highlightClass + '">' + w + '</small>'
					].join('')
				}).appendTo(weekContainer);

				week.days.forEach(day => {
					let isToday = today.formatDate(false) === day.formatDate(false);
					let inMonth = selectedDate.formatDate(true)[1] === day.formatDate(true)[1];
					let cellBackground = isToday ? 'bg-primary text-bg-primary' : (inMonth ? ' bg-white ' : ' bg-white fs-6 fw-small');
					let cellTextColor = inMonth ? 'var(--bs-dark)' : '#adadad';
					let highlight = !isToday ? '' : ' js-today ';
					let col = $('<div>', {
						'data-date': day.formatDate(false),
						class: 'position-relative d-flex border border-white rounded-circle justify-content-center align-items-center' + highlight + cellBackground,
						css: {
							color: cellTextColor,
							cursor: 'pointer',
							width: cellWidthHeight,
							height: cellWidthHeight
						},
						html: [
							'<div style="font-size:' + fontSize + 'px">' + day.formatDate(true)[2] + '</div>',
							[
								'<small class="js-count-events position-absolute text-center rounded-circle" style="width:4px; height:4px; bottom:4px; left: 50%; margin-left:-2px">',

								'</small>'
							].join('')
						].join('')
					}).appendTo(weekContainer);
					col.data('events', []);

				});
			});

			getEvents(container, selectedDate, function (events) {
				let haveEventsToday = false;
				events.forEach(event => {
					let start = new Date(event.start);
					let end = new Date(event.end);

					let curDate = start.clone();
					let currDaysFormatted;
					let now = new Date();
					do {

						currDaysFormatted = curDate.formatDate(false);

						if (false === haveEventsToday && (currDaysFormatted === now.formatDate(false))) {
							haveEventsToday = true;
						}

						let column = container.find('[data-date="' + curDate.formatDate(false) + '"]');
						if (column.length) {
							let dataEvents = column.data('events');
							dataEvents.push(event);
							let highlightClass = column.hasClass('js-today') ? 'bg-white' : 'bg-dark';
							column
								.data('events', dataEvents)
								.find(`.js-count-events`)
								.addClass(highlightClass)
						}
						curDate = curDate.clone().addDays(1);
					} while (currDaysFormatted < end.formatDate(false));
				});

				// alert();

				// if (clickDate !== null) {
				// 	container.find('[data-date="' + clickDate + '"]').trigger('click');
				// } else
					if (activeDate !== null && container.find('[data-date="' + activeDate + '"]').length) {
					container.find('[data-date="' + activeDate + '"]').trigger('click');
				} else {
					// Today is in the current month ?
					if (container.find('.js-today').length) {
						container.find('.js-today').trigger('click');
					}
				}
			});
		}

		/**
		 * @return {void}
		 */
		function events() {
			container
				.on('click', '.js-event', function (e) {
					let event = $(e.currentTarget).data('event');
					container.trigger('click-event', [event]);
				})
				.on('click', '.btn-prev-month', function (e) {
					e.preventDefault();

					current = current.clone().subMonths(1);
					drawCalendar(current);
					container.find('.js-collapse').hide(function () {
						container.triggerAll('click-prev-month change-month');
					});
				})
				.on('click', '.btn-curr-month', function (e) {
					e.preventDefault();

					current = today.clone();
					drawCalendar(current);
					container.find('.js-collapse').hide(function () {
						container.triggerAll('click-current-month change-month');
					});
				})
				.on('click', '.btn-next-month', function (e) {
					e.preventDefault();
					current = current.clone().addMonths(1);
					drawCalendar(current);
					container.find('.js-collapse').hide(function () {
						container.triggerAll('click-next-month change-month');
					});

				})
				.on('click', '[data-date]', function (e) {
					let $column = $(e.currentTarget);
					container
						.find('.js-weeks')
						.find('[data-date].border-secondary')
						.removeClass('border-secondary active')
						.addClass('border-white')
					$column.removeClass('border-white').addClass('border-secondary active')
					let date = new Date($column.data('date'));
					let events = $column.data('events');
					container.find('.js-day-name').html(date.showDateFormatted());
					drawEventList(container, events, date);
					container.find('.js-collapse').show();
					container.trigger('change-day', [date, events]);
				});
		}

		/**
		 *
		 * @param {object} container
		 * @param {array} events
		 * @param {Date} date
		 */
		function drawEventList(container, events, date) {
			container.trigger('show-event-list', [events]);
			let eventList = container.find('.js-events');
			eventList.empty();
			if (!events.length) {
				$('<div>', {
					class: 'list-group-item',
					html: settings.formatNoEvent(date)
				}).appendTo(eventList);
			} else {

				events.forEach(event => {
					let eventHtml = settings.formatEvent(event);

					let eventWrapper = $('<div>', {
						class: 'js-event d-flex justify-content-between align-items-center list-group-item p-0',
						html: `
                        <div class="flex-fill">${eventHtml}</div>
                        <div>
                            <div class="btn-group-vertical btn-group-sm" role="group" aria-label="Vertical button group"></div>
                        </div>
                       
                    `
					}).appendTo(eventList);
					getEventButtons(container, event, eventWrapper.find('.btn-group-vertical'));
					eventWrapper.data('event', event);
				});
			}

			setTimeout(function () {
				container.trigger('shown-event-list', [events]);
				eventList.find('[data-bs-toggle="tooltip"]').tooltip();
				eventList.find('[data-bs-toggle="popover"]').popover();
			}, 0);

		}

		function getEventButtons(container, event, wrap) {
			let settings = container.data('settings');
			let editable = !event.hasOwnProperty('editable') || event.editable;
			let deletable = !event.hasOwnProperty('deletable') || event.deletable;
			if (settings.showEventEditButton && editable) {
				let editButton = $('<a>', {
					href: '#',
					class: eventEditButton.class,
					html: `<i class="${settings.icons.eventEdit}"></i>`
				}).appendTo(wrap);

				editButton.on('click', function (e) {
					e.preventDefault();
					settings.onClickEditEvent(e, event);
				});
			}
			if (settings.showEventRemoveButton && deletable) {
				let removeButton = $('<a>', {
					href: '#',
					class: eventRemoveButton.class,
					html: `<i class="${settings.icons.eventRemove}"></i>`
				}).appendTo(wrap);

				removeButton.on('click', function (e) {
					e.preventDefault();
					settings.onClickDeleteEvent(e, event);
				});
			}
		}

		function init() {
			if (!container.data('init')) {
				drawCalendar(today);
				events();

				container.data('init', true);
				container.trigger('init');
			}
			return container;
		}

		if (isMethodSet) {
			switch (options) {
				case 'refresh':
					drawCalendar(current);
					break;
				// case 'setDate':
				// 	drawCalendar(current, params);
				// 	break;
			}
		}

		return init();
	};
}(jQuery));