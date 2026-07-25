(function () {
    "use strict";

    const dataElement = document.getElementById("dashboard-chart-data");
    const theme = window.OcpChartTheme;
    if (!dataElement ||
        typeof window.Chart !== "function" ||
        !theme) {
        return;
    }

    let dashboardData;
    try {
        dashboardData = JSON.parse(dataElement.textContent);
    } catch {
        return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const colors = theme.colors;
    const numberFormatter = theme.formatters.number;
    const decimalFormatter = theme.formatters.decimal;
    const p2o5Formatter = theme.formatters.p2o5;
    const percentageFormatter = theme.formatters.percentage;
    const dateFormatter = new Intl.DateTimeFormat("fr-FR", {
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

    function formatTripStatus(status) {
        const labels = {
            Completed: "Validé",
            InProgress: "En cours",
            PendingValidation: "À valider",
            Incomplete: "Incomplet",
            Planned: "Planifié",
            Cancelled: "Annulé"
        };

        return labels[status] ?? status;
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

    function commonOptions() {
        return theme.commonOptions(reducedMotion);
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
            options.plugins.tooltip = theme.tooltip({
                label: function (context) {
                    if (context.dataset.yAxisID === "tonnage") {
                        return `Tonnage complété : ${decimalFormatter.format(context.raw)} t`;
                    }

                    return `Voyages : ${numberFormatter.format(context.raw)}`;
                }
            });
            options.scales = {
                x: {
                    grid: { display: false },
                    border: { color: colors.border },
                    offset: true,
                    ticks: theme.ticks({
                        autoSkip: true,
                        maxTicksLimit: 12,
                        maxRotation: 0,
                        minRotation: 0
                    })
                },
                trips: {
                    beginAtZero: true,
                    position: "left",
                    title: {
                        display: true,
                        ...theme.axisTitle("Voyages")
                    },
                    grid: { color: colors.grid },
                    border: { color: colors.border },
                    ticks: theme.ticks({
                        precision: 0
                    })
                },
                tonnage: {
                    beginAtZero: true,
                    position: "right",
                    title: {
                        display: true,
                        ...theme.axisTitle("Tonnage (t)")
                    },
                    grid: { drawOnChartArea: false },
                    border: { color: colors.border },
                    ticks: theme.ticks()
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
                            backgroundColor: colors.greenFill,
                            hoverBackgroundColor: colors.greenHover,
                            borderColor: colors.green,
                            hoverBorderColor: colors.green,
                            borderWidth: 2,
                            barPercentage: 0.72,
                            categoryPercentage: 0.8,
                            maxBarThickness: 20
                        },
                        {
                            type: "line",
                            label: "Voyages",
                            data: daily.map(point => point.tripCount),
                            yAxisID: "trips",
                            borderColor: colors.blueGray,
                            backgroundColor: colors.blueGrayFill,
                            borderWidth: 3,
                            pointBackgroundColor: colors.blueGray,
                            pointBorderColor: colors.white,
                            pointStyle: "circle",
                            pointRadius: 3,
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
            options.plugins.tooltip = theme.tooltip({
                label: function (context) {
                    const count = Number(context.raw);
                    const percentage = formatPercentage(count, total);
                    return `${context.label} : ${numberFormatter.format(count)} voyages (${percentageFormatter.format(percentage)} %)`;
                }
            });

            return {
                type: "doughnut",
                data: {
                    labels: statuses.map(point => formatTripStatus(point.status)),
                    datasets: [{
                        label: "Voyages",
                        data: statuses.map(point => point.tripCount),
                        backgroundColor: statuses.map(
                            (_point, index) =>
                                theme.statusPalette[index % theme.statusPalette.length]),
                        borderColor: "#ffffff",
                        borderWidth: 2,
                        hoverBorderWidth: 2,
                        hoverOffset: 4,
                        spacing: 1
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
            options.plugins.ocpBarValueLabels = {
                formatter: value => `${decimalFormatter.format(value)} h`
            };
            options.plugins.tooltip = theme.tooltip({
                label: function (context) {
                    const point = stopReasons[context.dataIndex];
                    const stopLabel = point.stopCount === 1 ? "arrêt" : "arrêts";
                    return `${decimalFormatter.format(point.durationHours)} h (${numberFormatter.format(point.durationMinutes)} min) — ${numberFormatter.format(point.stopCount)} ${stopLabel}`;
                }
            });
            options.scales = {
                x: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        ...theme.axisTitle("Durée (h)")
                    },
                    grid: { color: colors.grid },
                    border: { color: colors.border },
                    ticks: theme.ticks()
                },
                y: {
                    grid: { display: false },
                    border: { color: colors.border },
                    ticks: theme.ticks({
                        autoSkip: false,
                        padding: 6,
                        font: {
                            family: "system-ui, -apple-system, sans-serif",
                            size: window.innerWidth < 576 ? 10 : 12,
                            weight: "600"
                        }
                    })
                }
            };

            return {
                type: "bar",
                data: {
                    labels: stopReasons.map(point => point.stopReasonName),
                    datasets: [{
                        label: "Durée (h)",
                        data: stopReasons.map(point => point.durationHours),
                        backgroundColor: colors.beigeFill,
                        hoverBackgroundColor: colors.beigeHover,
                        borderColor: colors.beige,
                        hoverBorderColor: colors.beige,
                        borderWidth: 2,
                        barPercentage: 0.72,
                        categoryPercentage: 0.8,
                        maxBarThickness: 26
                    }]
                },
                options,
                plugins: [theme.barValueLabelsPlugin]
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
            options.plugins.ocpBarValueLabels = {
                formatter: value => numberFormatter.format(value)
            };
            options.plugins.tooltip = theme.tooltip({
                label: context => `${numberFormatter.format(context.raw)} voyages`
            });
            options.scales = {
                x: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        ...theme.axisTitle("Voyages")
                    },
                    grid: { color: colors.grid },
                    border: { color: colors.border },
                    ticks: theme.ticks({
                        precision: 0
                    })
                },
                y: {
                    grid: { display: false },
                    border: { color: colors.border },
                    ticks: theme.ticks({
                        autoSkip: false,
                        padding: 6,
                        font: {
                            family: "system-ui, -apple-system, sans-serif",
                            size: window.innerWidth < 576 ? 10 : 12,
                            weight: "600"
                        }
                    })
                }
            };

            return {
                type: "bar",
                data: {
                    labels: trucks.map(point => point.truckCode),
                    datasets: [{
                        label: "Voyages",
                        data: trucks.map(point => point.tripCount),
                        backgroundColor: colors.blueGrayFill,
                        hoverBackgroundColor: colors.blueGrayHover,
                        borderColor: colors.blueGray,
                        hoverBorderColor: colors.blueGray,
                        borderWidth: 2,
                        barPercentage: 0.68,
                        categoryPercentage: 0.78,
                        maxBarThickness: 22
                    }]
                },
                options,
                plugins: [theme.barValueLabelsPlugin]
            };
        });
    }

    const p2o5 = dashboardData.p2O5Trend ?? [];
    if (p2o5.length > 0) {
        initializeChart("p2o5-trend-chart", "p2o5Trend", function () {
            const options = commonOptions();
            options.plugins.tooltip = theme.tooltip({
                label: context => `P2O5 : ${p2o5Formatter.format(context.raw)} %`
            });
            options.scales = {
                x: {
                    grid: { display: false },
                    border: { color: colors.border },
                    ticks: theme.ticks({
                        autoSkip: true,
                        maxTicksLimit: 12,
                        maxRotation: 0,
                        minRotation: 0
                    })
                },
                y: {
                    grace: "5%",
                    title: {
                        display: true,
                        ...theme.axisTitle("P2O5 (%)")
                    },
                    grid: { color: colors.grid },
                    border: { color: colors.border },
                    ticks: theme.ticks()
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
                        backgroundColor: colors.greenFill,
                        borderWidth: 3,
                        pointBackgroundColor: colors.green,
                        pointBorderColor: colors.white,
                        pointStyle: "circle",
                        pointRadius: 3,
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
