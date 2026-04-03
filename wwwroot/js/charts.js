// Thin Chart.js wrapper exposed as window.deadlockCharts so Blazor JS interop
// can render and dispose charts on demand. Each chart is keyed by canvas id.
//
// Sets Chart.js global defaults to match the Deadlock Dashboard dark theme on
// first load (axis labels, gridlines, legend text, tooltips). Individual chart
// configs can still override anything via standard Chart.js options.
(function () {
    const charts = new Map();

    // Palette mirrors --dl-* CSS variables in app.css. Kept here as literals so
    // Chart.js (which doesn't read CSS variables) gets concrete colors.
    const palette = {
        textPrimary:   "#EFDEBF",
        textSecondary: "#9A9AAE",
        textDisabled:  "#5E5E72",
        bgCard:        "#1A1A21",
        bgElevated:    "#2A2A38",
        borderDivider: "rgba(239,222,191,0.08)",
        borderDefault: "rgba(239,222,191,0.14)",
    };

    function applyDefaults() {
        if (typeof Chart === "undefined") return;
        Chart.defaults.font.family = "Segoe UI, system-ui, -apple-system, sans-serif";
        Chart.defaults.font.size = 12;
        Chart.defaults.color = palette.textSecondary;

        // Gridlines + axis tick lines.
        Chart.defaults.scale.grid.color = palette.borderDivider;
        Chart.defaults.scale.grid.tickColor = palette.borderDivider;
        Chart.defaults.scale.border = Chart.defaults.scale.border || {};
        Chart.defaults.scale.border.color = palette.borderDefault;
        Chart.defaults.scale.ticks.color = palette.textSecondary;
        Chart.defaults.scale.title.color = palette.textPrimary;
        Chart.defaults.scale.title.font = { weight: "600", size: 12 };

        // Legend & chart title.
        Chart.defaults.plugins.legend.labels.color = palette.textPrimary;
        Chart.defaults.plugins.legend.labels.font = { size: 12 };
        Chart.defaults.plugins.title.color = palette.textPrimary;
        Chart.defaults.plugins.title.font = { weight: "600", size: 13 };

        // Tooltip — elevated dark surface, accent border, monospace numerics.
        Chart.defaults.plugins.tooltip.backgroundColor = palette.bgElevated;
        Chart.defaults.plugins.tooltip.titleColor = palette.textPrimary;
        Chart.defaults.plugins.tooltip.bodyColor = palette.textPrimary;
        Chart.defaults.plugins.tooltip.borderColor = palette.borderDefault;
        Chart.defaults.plugins.tooltip.borderWidth = 1;
        Chart.defaults.plugins.tooltip.padding = 10;
        Chart.defaults.plugins.tooltip.cornerRadius = 4;
        Chart.defaults.plugins.tooltip.titleFont = { weight: "600", size: 12 };
        Chart.defaults.plugins.tooltip.bodyFont = {
            family: "Consolas, Courier New, monospace",
            size: 12,
        };
        Chart.defaults.plugins.tooltip.boxPadding = 4;
    }

    let defaultsApplied = false;
    function ensureDefaults() {
        if (defaultsApplied) return;
        applyDefaults();
        defaultsApplied = true;
    }

    // Walk through deadlockExtras on options/datasets and translate them into
    // real Chart.js callbacks. Extras are stripped from the live config object
    // before Chart.js sees it (Chart.js is fine with extra keys, but we keep
    // the configs clean and explicit).
    //
    // Recognised extras:
    //   options.deadlockExtras.tooltipSort = "desc" | "asc"
    //     -> sort tooltip items by their numeric value, greatest first / lowest first
    //   options.deadlockExtras.tooltipMeta = { datasetLabel: ["row 0 text", "row 1 text", ...] }
    //     -> append the per-point string after the value in the tooltip line
    //   options.deadlockExtras.onClickIndex = true
    //     -> when an element is clicked, invoke .NET method "OnPointClickedJs" on
    //        the DotNetObjectReference passed alongside the config
    function applyExtras(config, dotnetRef) {
        const extras = config?.options?.deadlockExtras;
        if (!extras) return;
        config.options = config.options || {};
        config.options.plugins = config.options.plugins || {};
        config.options.plugins.tooltip = config.options.plugins.tooltip || {};

        const tooltip = config.options.plugins.tooltip;

        if (extras.tooltipSort === "desc" || extras.tooltipSort === "asc") {
            tooltip.itemSort = function (a, b) {
                const av = Number(a.parsed?.y ?? a.parsed ?? 0);
                const bv = Number(b.parsed?.y ?? b.parsed ?? 0);
                return extras.tooltipSort === "desc" ? bv - av : av - bv;
            };
        }

        if (extras.tooltipMeta) {
            const meta = extras.tooltipMeta;
            tooltip.callbacks = tooltip.callbacks || {};
            tooltip.callbacks.label = function (ctx) {
                const baseLabel = ctx.dataset.label || "";
                const value = ctx.parsed?.y ?? ctx.parsed;
                const metaArr = meta[baseLabel];
                const extra = metaArr && ctx.dataIndex < metaArr.length ? metaArr[ctx.dataIndex] : "";
                let text = baseLabel ? `${baseLabel}: ${value}` : `${value}`;
                if (extra) text += ` — ${extra}`;
                return text;
            };
        }

        if (extras.onClickIndex && dotnetRef) {
            config.options.onClick = function (_evt, elements) {
                if (!elements || elements.length === 0) return;
                const idx = elements[0].index;
                dotnetRef.invokeMethodAsync("OnPointClickedJs", idx).catch(err => console.error(err));
            };
            // Pointer cursor when hovering a clickable bar.
            config.options.onHover = function (evt, elements) {
                const target = evt?.native?.target ?? evt?.target;
                if (target && target.style) {
                    target.style.cursor = (elements && elements.length > 0) ? "pointer" : "default";
                }
            };
        }

        // Strip the extras so Chart.js doesn't see them.
        delete config.options.deadlockExtras;
    }

    function render(canvasId, config, dotnetRef) {
        const el = document.getElementById(canvasId);
        if (!el) return;
        const existing = charts.get(canvasId);
        if (existing) {
            existing.destroy();
            charts.delete(canvasId);
        }
        if (typeof Chart === "undefined") {
            console.error("Chart.js failed to load from CDN");
            return;
        }
        ensureDefaults();
        applyExtras(config, dotnetRef);
        const chart = new Chart(el.getContext("2d"), config);
        charts.set(canvasId, chart);
    }

    function destroy(canvasId) {
        const existing = charts.get(canvasId);
        if (existing) {
            existing.destroy();
            charts.delete(canvasId);
        }
    }

    window.deadlockCharts = { render, destroy };
})();
