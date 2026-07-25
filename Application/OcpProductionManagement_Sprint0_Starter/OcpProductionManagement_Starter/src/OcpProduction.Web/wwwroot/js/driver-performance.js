(function () {
    "use strict";

    const dataElement = document.getElementById("driver-performance-chart-data");
    const theme = window.OcpChartTheme;
    if (!dataElement ||
        typeof window.Chart !== "function" ||
        !theme) {
        return;
    }

    let chartData;
    try {
        chartData = JSON.parse(dataElement.textContent);
    } catch {
        return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const colors = theme.colors;
    const numberFormatter = theme.formatters.number;
    const decimalFormatter = theme.formatters.decimal;

    function initializeChart(canvasId, fallbackName, createConfiguration) {
        const canvas = document.getElementById(canvasId);
        const visual = canvas?.closest(".performance-chart-visual");
        if (!canvas || !visual) {
            return;
        }

        visual.hidden = false;
        try {
            new window.Chart(canvas, createConfiguration());
            document.querySelector(
                `[data-chart-fallback="${fallbackName}"]`)?.removeAttribute("open");
        } catch {
            visual.hidden = true;
        }
    }

    function commonOptions(axisTitle) {
        const options = theme.commonOptions(reducedMotion);
        options.indexAxis = "y";
        options.layout.padding.right = window.innerWidth >= 768 ? 76 : 8;
        options.scales = {
            x: {
                beginAtZero: true,
                title: theme.axisTitle(axisTitle),
                grid: {
                    color: colors.grid,
                    drawTicks: true
                },
                border: {
                    color: colors.border
                },
                ticks: theme.ticks()
            },
            y: {
                grid: {
                    display: false
                },
                border: {
                    color: colors.border
                },
                ticks: theme.ticks({
                    autoSkip: false,
                    padding: 8,
                    font: {
                        family: "system-ui, -apple-system, sans-serif",
                        size: window.innerWidth < 576 ? 10 : 12,
                        weight: "600"
                    }
                })
            }
        };
        return options;
    }

    const trips = chartData.tripsByDriver ?? [];
    if (trips.length > 0) {
        initializeChart("driver-trips-chart", "driverTrips", function () {
            const options = commonOptions("Voyages");
            options.scales.x.ticks.precision = 0;
            options.plugins.legend = theme.legend();
            options.plugins.ocpBarValueLabels = {
                formatter: value => numberFormatter.format(value)
            };
            options.plugins.tooltip = theme.tooltip({
                label: context =>
                    `${context.dataset.label} : ${numberFormatter.format(Number(context.raw))}`
            });

            return {
                type: "bar",
                data: {
                    labels: trips.map(point => point.category),
                    datasets: [
                        {
                            label: "Voyages",
                            data: trips.map(point => point.totalTrips),
                            backgroundColor: colors.greenFill,
                            hoverBackgroundColor: colors.greenHover,
                            borderColor: colors.green,
                            hoverBorderColor: colors.green,
                            borderWidth: 2,
                            barPercentage: 0.74,
                            categoryPercentage: 0.82,
                            maxBarThickness: 20
                        },
                        {
                            label: "Voyages terminés",
                            data: trips.map(point => point.completedTrips),
                            backgroundColor: colors.blueGrayFill,
                            hoverBackgroundColor: colors.blueGrayHover,
                            borderColor: colors.blueGray,
                            hoverBorderColor: colors.blueGray,
                            borderWidth: 2,
                            barPercentage: 0.74,
                            categoryPercentage: 0.82,
                            maxBarThickness: 20
                        }
                    ]
                },
                options,
                plugins: [theme.barValueLabelsPlugin]
            };
        });
    }

    function initializeSingleSeries(
        canvasId,
        fallbackName,
        points,
        datasetLabel,
        axisTitle,
        formatter,
        color,
        fillColor,
        hoverColor,
        integerValues) {
        if (points.length === 0) {
            return;
        }

        initializeChart(canvasId, fallbackName, function () {
            const options = commonOptions(axisTitle);
            if (integerValues) {
                options.scales.x.ticks.precision = 0;
            }
            options.plugins.legend.display = false;
            options.plugins.ocpBarValueLabels = {
                formatter: value => formatter(value)
            };
            options.plugins.tooltip = theme.tooltip({
                label: context =>
                    `${datasetLabel} : ${formatter(Number(context.raw))}`
            });

            return {
                type: "bar",
                data: {
                    labels: points.map(point => point.category),
                    datasets: [{
                        label: datasetLabel,
                        data: points.map(point => point.value),
                        backgroundColor: fillColor,
                        hoverBackgroundColor: hoverColor,
                        borderColor: color,
                        hoverBorderColor: color,
                        borderWidth: 2,
                        barPercentage: 0.72,
                        categoryPercentage: 0.8,
                        maxBarThickness: 24
                    }]
                },
                options,
                plugins: [theme.barValueLabelsPlugin]
            };
        });
    }

    initializeSingleSeries(
        "driver-tonnage-chart",
        "driverTonnage",
        chartData.tonnageByDriver ?? [],
        "Tonnage transporté",
        "Tonnes",
        value => `${decimalFormatter.format(value)} t`,
        colors.blueGray,
        colors.blueGrayFill,
        colors.blueGrayHover,
        false);

    initializeSingleSeries(
        "driver-stops-chart",
        "driverStops",
        chartData.linkedStopsByDriver ?? [],
        "Arrêts associés",
        "Arrêts associés",
        value => numberFormatter.format(value),
        colors.beige,
        colors.beigeFill,
        colors.beigeHover,
        true);
})();
