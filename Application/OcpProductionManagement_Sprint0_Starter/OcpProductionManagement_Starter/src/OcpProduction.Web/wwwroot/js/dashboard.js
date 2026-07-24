(function () {
    "use strict";

    const dataElement = document.getElementById("dashboard-chart-data");
    if (!dataElement || typeof window.Chart !== "function") {
        return;
    }

    let dashboardData;
    try {
        dashboardData = JSON.parse(dataElement.textContent);
    } catch {
        return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const locale = "fr-FR";
    const colors = {
        green: "#176b45",
        greenSoft: "rgba(23, 107, 69, 0.24)",
        blue: "#315c6f",
        blueSoft: "rgba(49, 92, 111, 0.22)",
        brown: "#806744",
        brownSoft: "rgba(128, 103, 68, 0.24)",
        slate: "#5f6f76",
        slateSoft: "rgba(95, 111, 118, 0.22)",
        olive: "#66705b",
        oliveSoft: "rgba(102, 112, 91, 0.22)",
        neutral: "#59635f",
        neutralSoft: "rgba(89, 99, 95, 0.2)",
        grid: "rgba(63, 82, 75, 0.08)",
        text: "#42534d",
        textStrong: "#24312d"
    };
    colors.statusPalette = [
        colors.green,
        colors.blue,
        colors.brown,
        colors.slate,
        colors.olive,
        colors.neutral
    ];

    const numberFormatter = new Intl.NumberFormat(locale);
    const decimalFormatter = new Intl.NumberFormat(locale, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    const p2o5Formatter = new Intl.NumberFormat(locale, {
        minimumFractionDigits: 3,
        maximumFractionDigits: 3
    });
    const percentageFormatter = new Intl.NumberFormat(locale, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    const dateFormatter = new Intl.DateTimeFormat(locale, {
        day: "2-digit",
        month: "2-digit",
        year: "numeric"
    });

    function formatDate(value) {
        return dateFormatter.format(new Date(`${value}T00:00:00`));
    }

    function formatPercentage(value, total) {
        return total > 0 ? value * 100 / total : 0;
    }

    const statusCenterTextPlugin = {
        id: "dashboardStatusCenterText",
        afterDraw: function (chart, _args, options) {
            if (!chart.chartArea) {
                return;
            }

            const total = Number(options.total ?? 0);
            const centerX = (chart.chartArea.left + chart.chartArea.right) / 2;
            const centerY = (chart.chartArea.top + chart.chartArea.bottom) / 2;
            const numberSize = Math.max(20, Math.min(30, chart.width / 11));
            const labelSize = Math.max(11, Math.min(13, chart.width / 25));
            const context = chart.ctx;

            context.save();
            context.textAlign = "center";
            context.textBaseline = "middle";
            context.fillStyle = colors.textStrong;
            context.font = `700 ${numberSize}px system-ui, sans-serif`;
            context.fillText(numberFormatter.format(total), centerX, centerY - 8);
            context.fillStyle = colors.text;
            context.font = `600 ${labelSize}px system-ui, sans-serif`;
            context.fillText(total === 1 ? "voyage" : "voyages", centerX, centerY + 18);
            context.restore();
        }
    };

    const barValueLabelsPlugin = {
        id: "dashboardBarValueLabels",
        afterDatasetsDraw: function (chart, _args, options) {
            if (chart.width < 560 ||
                window.innerWidth < 768 ||
                typeof options.formatter !== "function") {
                return;
            }

            const metadata = chart.getDatasetMeta(0);
            if (metadata.hidden) {
                return;
            }

            const dataset = chart.data.datasets[0];
            const context = chart.ctx;
            context.save();
            context.fillStyle = colors.textStrong;
            context.font = "600 11px system-ui, sans-serif";
            context.textBaseline = "middle";

            metadata.data.forEach(function (bar, index) {
                const value = Number(dataset.data[index]);
                if (!Number.isFinite(value)) {
                    return;
                }

                const label = options.formatter(value, index);
                const labelWidth = context.measureText(label).width;
                const outsideX = bar.x + 7;
                const barWidth = Math.abs(bar.x - bar.base);

                if (outsideX + labelWidth <= chart.width - 8) {
                    context.textAlign = "left";
                    context.fillText(label, outsideX, bar.y);
                } else if (barWidth >= labelWidth + 18) {
                    context.textAlign = "right";
                    context.fillText(label, bar.x - 7, bar.y);
                }
            });

            context.restore();
        }
    };

    function commonOptions() {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: reducedMotion ? false : { duration: 300 },
            layout: {
                padding: {
                    top: 6,
                    right: 6,
                    bottom: 2,
                    left: 2
                }
            },
            interaction: {
                intersect: false,
                mode: "nearest"
            },
            plugins: {
                legend: {
                    display: true,
                    labels: {
                        color: colors.text,
                        usePointStyle: true,
                        boxWidth: 10,
                        padding: 16
                    }
                }
            }
        };
    }

    function initializeChart(canvasId, fallbackName, createConfiguration) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const visual = canvas.closest(".dashboard-chart-visual");
        if (!visual) {
            return;
        }

        visual.hidden = false;
        try {
            new window.Chart(canvas, createConfiguration());
            const fallback = document.querySelector(
                `[data-chart-fallback="${fallbackName}"]`);
            fallback?.removeAttribute("open");
        } catch {
            visual.hidden = true;
        }
    }

    const daily = dashboardData.dailyTripsAndTonnage ?? [];
    if (daily.length > 0) {
        initializeChart("daily-trips-chart", "dailyTrips", function () {
            const options = commonOptions();
            options.plugins.tooltip = {
                callbacks: {
                    label: function (context) {
                        if (context.dataset.yAxisID === "tonnage") {
                            return `Tonnage complété : ${decimalFormatter.format(context.raw)} t`;
                        }

                        return `Voyages : ${numberFormatter.format(context.raw)}`;
                    }
                }
            };
            options.scales = {
                x: {
                    grid: { display: false },
                    offset: true,
                    ticks: {
                        autoSkip: true,
                        maxTicksLimit: 12,
                        maxRotation: 0,
                        minRotation: 0,
                        padding: 8,
                        color: colors.text
                    }
                },
                trips: {
                    beginAtZero: true,
                    position: "left",
                    title: {
                        display: true,
                        text: "Voyages"
                    },
                    grid: { color: colors.grid },
                    ticks: {
                        color: colors.text,
                        precision: 0
                    }
                },
                tonnage: {
                    beginAtZero: true,
                    position: "right",
                    title: {
                        display: true,
                        text: "Tonnage (t)"
                    },
                    grid: { drawOnChartArea: false },
                    ticks: { color: colors.text }
                }
            };

            return {
                type: "bar",
                data: {
                    labels: daily.map(point => formatDate(point.date)),
                    datasets: [
                        {
                            type: "bar",
                            label: "Tonnage complété (t)",
                            data: daily.map(point => point.completedTonnage),
                            yAxisID: "tonnage",
                            backgroundColor: colors.greenSoft,
                            borderColor: colors.green,
                            borderWidth: 1,
                            barPercentage: 0.68,
                            categoryPercentage: 0.82,
                            maxBarThickness: 18
                        },
                        {
                            type: "line",
                            label: "Voyages",
                            data: daily.map(point => point.tripCount),
                            yAxisID: "trips",
                            borderColor: colors.blue,
                            backgroundColor: colors.blueSoft,
                            borderWidth: 2.5,
                            pointStyle: "rectRot",
                            pointRadius: 2,
                            pointHoverRadius: 5,
                            pointHitRadius: 10,
                            tension: 0,
                            spanGaps: false
                        }
                    ]
                },
                options
            };
        });
    }

    const statuses = dashboardData.tripsByStatus ?? [];
    if (statuses.length > 0) {
        initializeChart("trip-status-chart", "tripStatus", function () {
            const options = commonOptions();
            options.plugins.legend.display = false;
            const total = statuses.reduce(
                (sum, point) => sum + Number(point.tripCount),
                0);
            options.cutout = "68%";
            options.layout.padding = 10;
            options.plugins.dashboardStatusCenterText = { total };
            options.plugins.tooltip = {
                callbacks: {
                    label: function (context) {
                        const count = Number(context.raw);
                        const percentage = formatPercentage(count, total);
                        return `${context.label} : ${numberFormatter.format(count)} voyages (${percentageFormatter.format(percentage)} %)`;
                    }
                }
            };

            return {
                type: "doughnut",
                data: {
                    labels: statuses.map(point => point.status),
                    datasets: [{
                        label: "Voyages",
                        data: statuses.map(point => point.tripCount),
                        backgroundColor: statuses.map(
                            (_point, index) =>
                                colors.statusPalette[index % colors.statusPalette.length]),
                        borderColor: "#ffffff",
                        borderWidth: 1,
                        hoverBorderWidth: 1,
                        hoverOffset: 5
                    }]
                },
                options,
                plugins: [statusCenterTextPlugin]
            };
        });
    }

    const stopReasons = dashboardData.stopDurationByReason ?? [];
    if (stopReasons.length > 0) {
        initializeChart("stop-reason-chart", "stopReason", function () {
            const options = commonOptions();
            options.indexAxis = "y";
            options.layout.padding.right = window.innerWidth >= 768 ? 76 : 6;
            options.plugins.legend.display = false;
            options.plugins.dashboardBarValueLabels = {
                formatter: value => `${decimalFormatter.format(value)} h`
            };
            options.plugins.tooltip = {
                callbacks: {
                    label: function (context) {
                        const point = stopReasons[context.dataIndex];
                        const stopLabel = point.stopCount === 1 ? "arrêt" : "arrêts";
                        return `${decimalFormatter.format(point.durationHours)} h (${numberFormatter.format(point.durationMinutes)} min) — ${numberFormatter.format(point.stopCount)} ${stopLabel}`;
                    }
                }
            };
            options.scales = {
                x: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: "Durée (h)"
                    },
                    grid: { color: colors.grid },
                    ticks: { color: colors.text }
                },
                y: {
                    grid: { display: false },
                    ticks: {
                        autoSkip: false,
                        padding: 6,
                        color: colors.text,
                        font: {
                            size: window.innerWidth < 576 ? 10 : 12
                        }
                    }
                }
            };

            return {
                type: "bar",
                data: {
                    labels: stopReasons.map(point => point.stopReasonName),
                    datasets: [{
                        label: "Durée (h)",
                        data: stopReasons.map(point => point.durationHours),
                        backgroundColor: colors.brownSoft,
                        borderColor: colors.brown,
                        borderWidth: 1,
                        barPercentage: 0.68,
                        categoryPercentage: 0.82,
                        maxBarThickness: 24
                    }]
                },
                options,
                plugins: [barValueLabelsPlugin]
            };
        });
    }

    const trucks = dashboardData.truckActivity ?? [];
    if (trucks.length > 0) {
        initializeChart("truck-activity-chart", "truckActivity", function () {
            const options = commonOptions();
            options.indexAxis = "y";
            options.layout.padding.right = window.innerWidth >= 768 ? 48 : 6;
            options.plugins.legend.display = false;
            options.plugins.dashboardBarValueLabels = {
                formatter: value => numberFormatter.format(value)
            };
            options.plugins.tooltip = {
                callbacks: {
                    label: context => `${numberFormatter.format(context.raw)} voyages`
                }
            };
            options.scales = {
                x: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: "Voyages"
                    },
                    grid: { color: colors.grid },
                    ticks: {
                        color: colors.text,
                        precision: 0
                    }
                },
                y: {
                    grid: { display: false },
                    ticks: {
                        autoSkip: false,
                        padding: 6,
                        color: colors.text,
                        font: {
                            size: window.innerWidth < 576 ? 10 : 12
                        }
                    }
                }
            };

            return {
                type: "bar",
                data: {
                    labels: trucks.map(point => point.truckCode),
                    datasets: [{
                        label: "Voyages",
                        data: trucks.map(point => point.tripCount),
                        backgroundColor: colors.blueSoft,
                        borderColor: colors.blue,
                        borderWidth: 1,
                        barPercentage: 0.62,
                        categoryPercentage: 0.78,
                        maxBarThickness: 18
                    }]
                },
                options,
                plugins: [barValueLabelsPlugin]
            };
        });
    }

    const p2o5 = dashboardData.p2O5Trend ?? [];
    if (p2o5.length > 0) {
        initializeChart("p2o5-trend-chart", "p2o5Trend", function () {
            const options = commonOptions();
            options.plugins.tooltip = {
                callbacks: {
                    label: context => `P2O5 : ${p2o5Formatter.format(context.raw)} %`
                }
            };
            options.scales = {
                x: {
                    grid: { display: false },
                    ticks: {
                        autoSkip: true,
                        maxTicksLimit: 12,
                        maxRotation: 0,
                        minRotation: 0,
                        padding: 8,
                        color: colors.text
                    }
                },
                y: {
                    grace: "5%",
                    title: {
                        display: true,
                        text: "P2O5 (%)"
                    },
                    grid: { color: colors.grid },
                    ticks: { color: colors.text }
                }
            };

            return {
                type: "line",
                data: {
                    labels: p2o5.map(point => formatDate(point.date)),
                    datasets: [{
                        label: "P2O5 moyen (%)",
                        data: p2o5.map(point => point.averageP2O5),
                        borderColor: colors.green,
                        backgroundColor: colors.greenSoft,
                        borderWidth: 2.5,
                        pointStyle: "circle",
                        pointRadius: 2,
                        pointHoverRadius: 5,
                        pointHitRadius: 10,
                        tension: 0,
                        spanGaps: false,
                        fill: false
                    }]
                },
                options
            };
        });
    }
})();
