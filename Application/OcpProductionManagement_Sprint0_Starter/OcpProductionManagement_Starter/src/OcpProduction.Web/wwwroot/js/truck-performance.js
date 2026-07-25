(function () {
    "use strict";

    const dataElement = document.getElementById("truck-performance-chart-data");
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
    const p2o5Formatter = theme.formatters.p2o5;

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

    function createHorizontalBarConfiguration(
        points,
        datasetLabel,
        axisTitle,
        formatter,
        color,
        fillColor,
        hoverColor) {
        const options = theme.commonOptions(reducedMotion);
        options.indexAxis = "y";
        options.layout.padding.right = window.innerWidth >= 768 ? 82 : 8;
        options.plugins.legend.display = false;
        options.plugins.ocpBarValueLabels = {
            formatter: value => formatter(value)
        };
        options.plugins.tooltip = theme.tooltip({
            label: context =>
                `${datasetLabel} : ${formatter(Number(context.raw))}`
        });
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
                    maxBarThickness: 26
                }]
            },
            options,
            plugins: [theme.barValueLabelsPlugin]
        };
    }

    const trips = chartData.tripsByTruck ?? [];
    if (trips.length > 0) {
        initializeChart("truck-trips-chart", "truckTrips", function () {
            const configuration = createHorizontalBarConfiguration(
                trips,
                "Voyages",
                "Voyages",
                value => numberFormatter.format(value),
                colors.green,
                colors.greenFill,
                colors.greenHover);
            configuration.options.scales.x.ticks.precision = 0;
            return configuration;
        });
    }

    const tonnage = chartData.tonnageByTruck ?? [];
    if (tonnage.length > 0) {
        initializeChart("truck-tonnage-chart", "truckTonnage", function () {
            return createHorizontalBarConfiguration(
                tonnage,
                "Tonnage transporté",
                "Tonnes",
                value => `${decimalFormatter.format(value)} t`,
                colors.blueGray,
                colors.blueGrayFill,
                colors.blueGrayHover);
        });
    }

    const stops = chartData.stopsByTruck ?? [];
    if (stops.length > 0) {
        initializeChart("truck-stops-chart", "truckStops", function () {
            const configuration = createHorizontalBarConfiguration(
                stops,
                "Arrêts",
                "Arrêts",
                value => numberFormatter.format(value),
                colors.beige,
                colors.beigeFill,
                colors.beigeHover);
            configuration.options.scales.x.ticks.precision = 0;
            return configuration;
        });
    }

    const p2o5 = chartData.p2O5ByTruck ?? [];
    if (p2o5.length > 0) {
        initializeChart("truck-p2o5-chart", "truckP2O5", function () {
            const configuration = createHorizontalBarConfiguration(
                p2o5,
                "P2O5 moyen observé",
                "P2O5 (%)",
                value => `${p2o5Formatter.format(value)} %`,
                colors.greenDark,
                colors.greenFill,
                colors.greenHover);
            configuration.options.scales.x.beginAtZero = false;
            configuration.options.scales.x.grace = "5%";
            return configuration;
        });
    }
})();
