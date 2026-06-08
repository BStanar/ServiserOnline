(function (global) {
    'use strict';

    const DEFAULT_PDFMAKE_URL = 'https://cdnjs.cloudflare.com/ajax/libs/pdfmake/0.2.7/pdfmake.min.js';
    const DEFAULT_VFS_URL = 'https://cdnjs.cloudflare.com/ajax/libs/pdfmake/0.2.7/vfs_fonts.js';

    function loadScript(src) {
        return new Promise(function (resolve, reject) {
            const existing = document.querySelector('script[src="' + src + '"]');
            if (existing) {
                if (existing.dataset.loaded === 'true') {
                    resolve();
                    return;
                }
                existing.addEventListener('load', function () { resolve(); }, { once: true });
                existing.addEventListener('error', function () { reject(new Error('Ne mogu učitati script: ' + src)); }, { once: true });
                return;
            }

            const s = document.createElement('script');
            s.src = src;
            s.async = true;
            s.onload = function () {
                s.dataset.loaded = 'true';
                resolve();
            };
            s.onerror = function () {
                reject(new Error('Ne mogu učitati script: ' + src));
            };
            document.head.appendChild(s);
        });
    }

    async function ensurePdfMake(options) {
        options = options || {};

        if (typeof global.pdfMake === 'undefined') {
            await loadScript(options.pdfMakeUrl || DEFAULT_PDFMAKE_URL);
        }

        if (!global.pdfMake || !global.pdfMake.vfs) {
            await loadScript(options.vfsFontsUrl || DEFAULT_VFS_URL);
        }

        if (typeof global.pdfMake === 'undefined') {
            throw new Error('pdfMake nije učitan.');
        }
    }

    function imageToDataUrl(url) {
        return new Promise(function (resolve) {
            if (!url) {
                resolve(null);
                return;
            }

            const img = new Image();
            img.crossOrigin = 'anonymous';

            img.onload = function () {
                try {
                    const canvas = document.createElement('canvas');
                    canvas.width = img.naturalWidth;
                    canvas.height = img.naturalHeight;
                    canvas.getContext('2d').drawImage(img, 0, 0);
                    resolve(canvas.toDataURL('image/png'));
                } catch (_) {
                    resolve(null);
                }
            };

            img.onerror = function () {
                resolve(null);
            };

            img.src = url;
        });
    }

    function text(value) {
        if (value === null || value === undefined) return '';
        return String(value);
    }

    function fileSafe(value) {
        const raw = text(value || 'bez-broja').trim();
        return (raw || 'bez-broja').replace(/[\\/:*?"<>|]/g, '-');
    }

    function chk(checked, label) {
        return {
            columns: [
                {
                    canvas: checked ? [
                        { type: 'rect', x: 0, y: 1, w: 10, h: 10, r: 1, lineWidth: 1, lineColor: '#3a5a8a', color: '#3a5a8a' },
                        { type: 'line', x1: 2, y1: 6, x2: 4, y2: 9, lineWidth: 1.5, lineColor: '#fff' },
                        { type: 'line', x1: 4, y1: 9, x2: 9, y2: 2, lineWidth: 1.5, lineColor: '#fff' }
                    ] : [
                        { type: 'rect', x: 0, y: 1, w: 10, h: 10, r: 1, lineWidth: 1, lineColor: '#3a5a8a' }
                    ],
                    width: 14
                },
                { text: label, fontSize: 10.5, margin: [2, 1, 0, 0] }
            ],
            margin: [0, 0, 0, 3]
        };
    }

    function valueCell(value, extra) {
        return Object.assign({ text: text(value), style: 'value' }, extra || {});
    }

    function labelCell(value, extra) {
        return Object.assign({ text: text(value), style: 'label' }, extra || {});
    }

    function buildDeviceRows(data) {
        const devices = Array.isArray(data.devices) ? data.devices : [];

        if (!devices.length) {
            return [[{ text: '-', colSpan: 4, color: '#999' }, {}, {}, {}]];
        }

        return devices.map(function (item) {
            return [
                valueCell(item.modelName),
                valueCell(item.manufacturerName),
                valueCell(item.serialNumber),
                valueCell(item.locationName)
            ];
        });
    }

    function buildSpareRows(data) {
        const rows = [
            [
                labelCell('Kataloški br.'),
                labelCell('Naziv dijela'),
                labelCell('Količina'),
                labelCell('Napomena')
            ]
        ];

        const spares = Array.isArray(data.spareParts) ? data.spareParts : [];

        if (!spares.length) {
            rows.push([{ text: '-', colSpan: 4, color: '#999' }, {}, {}, {}]);
            return rows;
        }

        spares.forEach(function (item) {
            rows.push([
                { text: text(item.catalogNumber || item.serialNumber) },
                { text: text(item.name) },
                { text: text(item.amount) },
                { text: text(item.note) }
            ]);
        });

        return rows;
    }

    async function buildDocDefinition(data, options) {
        data = data || {};
        options = options || {};

        const logoDataUrl = await imageToDataUrl(options.logoUrl || data.logoUrl || '/images/Memorandum.png');
        const logoColumn = logoDataUrl ? { image: logoDataUrl, width: 350 } : { text: '', width: 150 };
        const deviceRows = buildDeviceRows(data);
        const sparePartRows = buildSpareRows(data);
        const hasSpares = Array.isArray(data.spareParts) && data.spareParts.length > 0;

        const customLayout = {
            hLineWidth: function () { return 0.5; },
            vLineWidth: function () { return 0.5; },
            hLineColor: function () { return '#aaa'; },
            vLineColor: function () { return '#aaa'; },
            paddingLeft: function () { return 6; },
            paddingRight: function () { return 6; },
            paddingTop: function () { return 4; },
            paddingBottom: function () { return 4; }
        };

        const body = [
            [
                labelCell('Servisni izvještaj br.'),
                labelCell('Datum posjete'),
                labelCell('Datum poziva'),
                labelCell('Vrsta usluge')
            ],
            [
                valueCell(data.caseServisNumber),
                valueCell(data.dateTimePlanned),
                valueCell(data.dateTimeCaseOpened),
                valueCell(data.interventionTypeName)
            ],
            [
                labelCell('Ime servisera', { colSpan: 2 }), {},
                labelCell('Sati puta'),
                labelCell('Sati rada')
            ],
            [
                valueCell(data.servicePerson, { colSpan: 2 }), {},
                valueCell(data.hoursOfTravel),
                valueCell(data.hoursOfWork)
            ],
            [
                labelCell('Korisnik usluga', { colSpan: 2 }), {},
                labelCell('Mjesto'),
                labelCell('Adresa')
            ],
            [
                valueCell(data.clientName, { colSpan: 2, bold: true }), {},
                valueCell(data.clientCity),
                valueCell(data.clientAddress)
            ],
            [
                labelCell('Prisutna osoba', { colSpan: 2 }), {},
                labelCell('Broj ugovora / narudžbenice', { colSpan: 2 }), {}
            ],
            [
                valueCell(data.attendingPerson, { colSpan: 2 }), {},
                valueCell(data.contractNo, { colSpan: 2 }), {}
            ],
            [{ text: 'INSTRUMENTI', style: 'section', colSpan: 4 }, {}, {}, {}],
            [
                labelCell('Model instrumenta'),
                labelCell('Proizvođač'),
                labelCell('Serijski broj'),
                labelCell('Odjel / lokacija')
            ],
            ...deviceRows,
            [{ text: 'OPIS USLUGE', style: 'section', colSpan: 4 }, {}, {}, {}],
            [valueCell(data.serviceDescription, { colSpan: 4 }), {}, {}, {}],
            [{ text: 'OPIS IZVRŠENE INTERVENCIJE', style: 'section', colSpan: 4 }, {}, {}, {}],
            [valueCell(data.interventionDescription, { colSpan: 4 }), {}, {}, {}]
        ];

        if (hasSpares) {
            body.push(
                [{ text: 'UPOTREBLJENI DIJELOVI', style: 'section', colSpan: 4 }, {}, {}, {}],
                ...sparePartRows
            );
        }

        body.push(
            [
                labelCell('Nedovršeno', { colSpan: 2 }), {},
                labelCell('Intervencija završena'),
                labelCell('Nastavak intervencije')
            ],
            [
                valueCell(data.notFinishedDescription, { colSpan: 2 }), {},
                {
                    stack: [
                        chk(!!data.finished, 'Da'),
                        chk(!data.finished, 'Ne')
                    ]
                },
                valueCell(data.continueFromNo)
            ],
            [{ text: 'Naš predstavnik obavio je uslugu na zadovoljavajući način', style: 'banner', colSpan: 4 }, {}, {}, {}],
            [
                labelCell('Datum'),
                labelCell('Potpis korisnika usluge'),
                labelCell('Odobrio'),
                labelCell('Plaćanje')
            ],
            [
                valueCell(data.dateTimeOfReport),
                { text: '\n\nPotpis / pečat', color: '#999' },
                { text: '\n\nPotpis', color: '#999' },
                {
                    stack: [
                        chk(data.payWhen === 'PayNow' || data.payNow === true, 'Naplati odmah'),
                        chk(data.payWhen === 'PayLater' || data.payLater === true, 'Naplati kasnije'),
                        chk(data.payWhen === 'NoPay' || data.noPay === true, 'Bez naplate')
                    ]
                }
            ]
        );

        return {
            pageSize: 'A4',
            pageMargins: [30, 30, 30, 30],
            styles: {
                section: { fontSize: 8, bold: true, color: '#ffffff', fillColor: '#3a5a8a' },
                label: { fontSize: 7.5, bold: true, color: '#3a5a8a', fillColor: '#eef2fa' },
                value: { fontSize: 10.5 },
                banner: { fontSize: 9, bold: true, color: '#ffffff', fillColor: '#2b4570', alignment: 'center' }
            },
            content: [
                {
                    columns: [
                        logoColumn,
                        {
                            alignment: 'right',
                            stack: [
                                { text: 'Servisni izvještaj', fontSize: 18, bold: true },
                                { text: 'Br. izvještaja: ' + text(data.caseServisNumber), fontSize: 10, color: '#666' }
                            ]
                        }
                    ],
                    margin: [0, 0, 0, 10]
                },
                {
                    table: {
                        widths: ['16%', '16%', '16%', '52%'],
                        body: body
                    },
                    layout: customLayout
                }
            ]
        };
    }

    async function createPdf(data, options) {
        await ensurePdfMake(options || {});
        const docDefinition = await buildDocDefinition(data, options || {});
        return global.pdfMake.createPdf(docDefinition);
    }

    async function download(data, options) {
        options = options || {};
        const pdf = await createPdf(data, options);
        const fileName = options.fileName || 'Servisni-izvjestaj-' + fileSafe(data && data.caseServisNumber) + '.pdf';
        pdf.download(fileName);
    }

    async function open(data, options) {
        options = options || {};
        const pdf = await createPdf(data, options);
        pdf.open();
    }

    global.ServiceReportPdf = {
        buildDocDefinition: buildDocDefinition,
        createPdf: createPdf,
        download: download,
        open: open
    };
})(window);
