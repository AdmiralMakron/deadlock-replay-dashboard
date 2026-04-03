// Chart.js interop for Blazor — dark theme
window.deadlockCharts = {
    _charts: {},

    // Palette tokens
    _theme: {
        bgCard: '#1A1A21',
        bgElevated: '#2A2A38',
        textPrimary: '#EFDEBF',
        textSecondary: '#9A9AAE',
        textDisabled: '#5E5E72',
        gridLine: 'rgba(239,222,191,0.06)',
        gridLineBright: 'rgba(239,222,191,0.10)',
        tooltipBg: '#2A2A38',
        tooltipBorder: 'rgba(239,222,191,0.14)',
        fontBody: "'Segoe UI', system-ui, -apple-system, sans-serif",
        fontMono: "'Consolas', 'Courier New', monospace",
        series: ['#E8943A','#5DAA68','#9B6FD4','#3A7BD5','#D4A843','#4D9D8F','#D43A3A','#C9ABF0']
    },

    createLineChart: function (canvasId, config) {
        this.destroyChart(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        const t = this._theme;

        const datasets = config.datasets.map((ds, i) => ({
            label: ds.label,
            data: ds.data,
            borderColor: ds.color,
            backgroundColor: ds.color + '22',
            borderWidth: 2,
            pointRadius: 0,
            pointHoverRadius: 4,
            pointHitRadius: 10,
            tension: 0.35,
            hidden: ds.hidden || false
        }));

        this._charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: { labels: config.labels, datasets: datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                        labels: {
                            color: t.textSecondary,
                            boxWidth: 10,
                            boxHeight: 10,
                            padding: 12,
                            font: { family: t.fontBody, size: 11 },
                            usePointStyle: true,
                            pointStyle: 'circle'
                        }
                    },
                    tooltip: {
                        backgroundColor: t.tooltipBg,
                        titleColor: t.textPrimary,
                        bodyColor: t.textSecondary,
                        borderColor: t.tooltipBorder,
                        borderWidth: 1,
                        padding: 10,
                        titleFont: { family: t.fontBody, size: 12, weight: '600' },
                        bodyFont: { family: t.fontMono, size: 12 },
                        cornerRadius: 6,
                        itemSort: function(a, b) {
                            return (b.raw || 0) - (a.raw || 0);
                        },
                        callbacks: {
                            label: function(ctx) {
                                return ' ' + ctx.dataset.label + ':  ' + (ctx.raw || 0).toLocaleString();
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: config.xLabel || 'Game Time (min)',
                            color: t.textDisabled,
                            font: { family: t.fontBody, size: 11, weight: '600' }
                        },
                        ticks: {
                            color: t.textDisabled,
                            maxTicksLimit: 20,
                            font: { family: t.fontBody, size: 11 },
                            callback: function(value, index, ticks) {
                                var label = this.getLabelForValue(value);
                                var num = parseFloat(label);
                                if (isNaN(num)) return label;
                                return Math.round(num) + 'm';
                            }
                        },
                        grid: { color: t.gridLine, drawBorder: false }
                    },
                    y: {
                        title: {
                            display: true,
                            text: config.yLabel || 'Souls',
                            color: t.textDisabled,
                            font: { family: t.fontBody, size: 11, weight: '600' }
                        },
                        ticks: {
                            color: t.textDisabled,
                            font: { family: t.fontMono, size: 11 },
                            callback: function(value) {
                                if (value >= 1000) return (value / 1000).toFixed(0) + 'k';
                                return value;
                            }
                        },
                        grid: { color: t.gridLineBright, drawBorder: false },
                        beginAtZero: true
                    }
                }
            }
        });
    },

    createBarChart: function (canvasId, config) {
        this.destroyChart(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        const t = this._theme;

        const datasets = config.datasets.map(ds => ({
            label: ds.label,
            data: ds.data,
            backgroundColor: ds.colors || ds.color,
            borderColor: 'transparent',
            borderWidth: 0,
            borderRadius: 3,
            barPercentage: 0.7,
            categoryPercentage: 0.8
        }));

        this._charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: { labels: config.labels, datasets: datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: config.horizontal ? 'y' : 'x',
                plugins: {
                    legend: {
                        display: config.datasets.length > 1,
                        position: 'top',
                        labels: {
                            color: t.textSecondary,
                            boxWidth: 10,
                            boxHeight: 10,
                            padding: 12,
                            font: { family: t.fontBody, size: 11 },
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        backgroundColor: t.tooltipBg,
                        titleColor: t.textPrimary,
                        bodyColor: t.textSecondary,
                        borderColor: t.tooltipBorder,
                        borderWidth: 1,
                        padding: 10,
                        titleFont: { family: t.fontBody, size: 12, weight: '600' },
                        bodyFont: { family: t.fontMono, size: 12 },
                        cornerRadius: 6,
                        callbacks: {
                            label: function(ctx) {
                                return ' ' + ctx.dataset.label + ':  ' + (ctx.raw || 0).toLocaleString();
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: {
                            color: t.textDisabled,
                            font: { family: t.fontBody, size: 11 },
                            maxRotation: 45
                        },
                        grid: { color: t.gridLine, drawBorder: false }
                    },
                    y: {
                        ticks: {
                            color: t.textDisabled,
                            font: { family: t.fontMono, size: 11 },
                            callback: function(value) {
                                if (value >= 1000) return (value / 1000).toFixed(0) + 'k';
                                return value;
                            }
                        },
                        grid: { color: t.gridLineBright, drawBorder: false },
                        beginAtZero: true
                    }
                }
            }
        });
    },

    updateChart: function (canvasId, config) {
        var chart = this._charts[canvasId];
        if (!chart) {
            if (config.type === 'bar') {
                this.createBarChart(canvasId, config);
            } else {
                this.createLineChart(canvasId, config);
            }
            return;
        }
        chart.data.labels = config.labels;
        chart.data.datasets = config.datasets.map(function(ds) {
            return {
                label: ds.label,
                data: ds.data,
                borderColor: ds.color,
                backgroundColor: config.type === 'bar' ? (ds.colors || ds.color) : (ds.color + '22'),
                borderWidth: config.type === 'bar' ? 0 : 2,
                pointRadius: config.type === 'bar' ? undefined : 0,
                tension: config.type === 'bar' ? undefined : 0.35,
                hidden: ds.hidden || false,
                borderRadius: config.type === 'bar' ? 3 : undefined
            };
        });
        chart.update();
    },

    destroyChart: function (canvasId) {
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }
    }
};
