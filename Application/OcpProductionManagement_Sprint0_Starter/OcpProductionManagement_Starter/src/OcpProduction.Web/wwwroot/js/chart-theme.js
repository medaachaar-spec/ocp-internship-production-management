(function () {
    "use strict";

    const locale = "fr-FR";
    const colors = Object.freeze({
        green: "#176b45",
        greenDark: "#104c32",
        greenFill: "#b9d8c8",
        greenHover: "#91bda5",
        blueGray: "#315c6f",
        blueGrayFill: "#bed1da",
        blueGrayHover: "#91afbd",
        blue: "#426f86",
        blueFill: "#c7d8e0",
        blueHover: "#9bb9c7",
        beige: "#9a7848",
        beigeFill: "#dfd0b9",
        beigeHover: "#c8b18e",
        slate: "#596b73",
        slateFill: "#cbd4d7",
        olive: "#65725b",
        oliveFill: "#d1d7c9",
        neutral: "#5b6460",
        neutralFill: "#d5d9d7",
        text: "#52625c",
        textStrong: "#24312d",
        grid: "rgba(36, 49, 45, 0.12)",
        border: "#d8e2dd",
        tooltip: "rgba(20, 56, 44, 0.97)",
        white: "#ffffff"
    });

    const formatters = Object.freeze({
        number: new Intl.NumberFormat(locale),
        decimal: new Intl.NumberFormat(locale, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }),
        p2o5: new Intl.NumberFormat(locale, {
            minimumFractionDigits: 3,
            maximumFractionDigits: 3
        }),
        percentage: new Intl.NumberFormat(locale, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        })
    });

    function tooltip(callbacks) {
        return {
            backgroundColor: colors.tooltip,
            titleColor: colors.white,
            bodyColor: colors.white,
            borderColor: "#49675c",
            borderWidth: 1,
            padding: 11,
            cornerRadius: 6,
            titleSpacing: 4,
            bodySpacing: 5,
            displayColors: true,
            boxPadding: 4,
            callbacks
        };
    }

    function legend() {
        return {
            display: true,
            labels: {
                color: colors.text,
                usePointStyle: true,
                pointStyleWidth: 10,
                boxWidth: 10,
                boxHeight: 10,
                padding: 18,
                font: {
                    family: "system-ui, -apple-system, sans-serif",
                    size: 12,
                    weight: "600"
                }
            }
        };
    }

    function axisTitle(text) {
        return {
            display: true,
            text,
            color: colors.textStrong,
            font: {
                family: "system-ui, -apple-system, sans-serif",
                size: 12,
                weight: "600"
            },
            padding: {
                top: 8,
                bottom: 4
            }
        };
    }

    function ticks(overrides) {
        return Object.assign({
            color: colors.text,
            padding: 8,
            font: {
                family: "system-ui, -apple-system, sans-serif",
                size: 11,
                weight: "500"
            }
        }, overrides);
    }

    function commonOptions(reducedMotion) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            locale,
            resizeDelay: 100,
            animation: reducedMotion ? false : { duration: 240 },
            layout: {
                padding: {
                    top: 8,
                    right: 8,
                    bottom: 4,
                    left: 4
                }
            },
            interaction: {
                intersect: false,
                mode: "nearest"
            },
            elements: {
                bar: {
                    borderSkipped: false,
                    borderRadius: 3
                },
                line: {
                    borderCapStyle: "round",
                    borderJoinStyle: "round"
                },
                point: {
                    borderWidth: 2,
                    hoverBorderWidth: 2
                }
            },
            plugins: {
                legend: legend()
            }
        };
    }

    const barValueLabelsPlugin = {
        id: "ocpBarValueLabels",
        afterDatasetsDraw: function (chart, _args, options) {
            if (chart.width < 560 ||
                window.innerWidth < 768 ||
                typeof options.formatter !== "function") {
                return;
            }

            const context = chart.ctx;
            context.save();
            context.fillStyle = colors.textStrong;
            context.font = "600 11px system-ui, -apple-system, sans-serif";
            context.textBaseline = "middle";

            chart.data.datasets.forEach(function (dataset, datasetIndex) {
                const metadata = chart.getDatasetMeta(datasetIndex);
                if (metadata.hidden) {
                    return;
                }

                metadata.data.forEach(function (bar, dataIndex) {
                    const value = Number(dataset.data[dataIndex]);
                    if (!Number.isFinite(value) || !Number.isFinite(bar.base)) {
                        return;
                    }

                    const label = options.formatter(
                        value,
                        dataIndex,
                        datasetIndex);
                    const labelWidth = context.measureText(label).width;
                    const outsideX = bar.x + 8;
                    const barWidth = Math.abs(bar.x - bar.base);

                    if (outsideX + labelWidth <= chart.width - 8) {
                        context.textAlign = "left";
                        context.fillText(label, outsideX, bar.y);
                    } else if (barWidth >= labelWidth + 20) {
                        context.textAlign = "right";
                        context.fillStyle = colors.white;
                        context.fillText(label, bar.x - 8, bar.y);
                        context.fillStyle = colors.textStrong;
                    }
                });
            });

            context.restore();
        }
    };

    window.OcpChartTheme = Object.freeze({
        colors,
        formatters,
        statusPalette: Object.freeze([
            colors.green,
            colors.blueGray,
            colors.beige,
            colors.slate,
            colors.olive,
            colors.neutral
        ]),
        tooltip,
        legend,
        axisTitle,
        ticks,
        commonOptions,
        barValueLabelsPlugin
    });
})();
