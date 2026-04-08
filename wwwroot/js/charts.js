window.deadlockCharts = {
    _colors: {
        bg: '#0F0F13',
        cardBg: '#1A1A21',
        text: '#EFDEBF',
        textSec: '#9A9AAE',
        grid: 'rgba(239,222,191,0.08)',
        amber: '#E8943A',
        sapphire: '#3A7BD5',
        green: '#5DAA68',
        red: '#D43A3A'
    },

    _setupCanvas(id, extraHeight) {
        const canvas = document.getElementById(id);
        if (!canvas) return null;
        const ctx = canvas.getContext('2d');
        const dpr = window.devicePixelRatio || 1;
        const rect = canvas.parentElement.getBoundingClientRect();
        const w = rect.width - 2;
        const h = extraHeight || 300;
        canvas.width = w * dpr;
        canvas.height = h * dpr;
        canvas.style.width = w + 'px';
        canvas.style.height = h + 'px';
        ctx.scale(dpr, dpr);
        return { ctx, w, h };
    },

    renderLineChart(id, labels, series) {
        const setup = this._setupCanvas(id, 350);
        if (!setup) return;
        const { ctx, w, h } = setup;
        const c = this._colors;
        const pad = { top: 20, right: 20, bottom: 40, left: 65 };
        const plotW = w - pad.left - pad.right;
        const plotH = h - pad.top - pad.bottom;

        ctx.fillStyle = c.cardBg;
        ctx.fillRect(0, 0, w, h);

        if (!series.length || !labels.length) return;

        let maxY = 0;
        series.forEach(s => s.data.forEach(v => { if (v > maxY) maxY = v; }));
        if (maxY === 0) maxY = 1;

        // Grid
        const ySteps = 5;
        ctx.strokeStyle = c.grid;
        ctx.lineWidth = 1;
        ctx.font = '12px Consolas, Courier New, monospace';
        ctx.fillStyle = c.textSec;
        ctx.textAlign = 'right';
        for (let i = 0; i <= ySteps; i++) {
            const y = pad.top + plotH - (i / ySteps) * plotH;
            ctx.beginPath();
            ctx.moveTo(pad.left, y);
            ctx.lineTo(w - pad.right, y);
            ctx.stroke();
            const val = Math.round((i / ySteps) * maxY);
            ctx.fillText(val >= 1000 ? (val/1000).toFixed(1)+'k' : val, pad.left - 8, y + 4);
        }

        // X axis
        ctx.textAlign = 'center';
        const xStep = Math.max(1, Math.floor(labels.length / 10));
        for (let i = 0; i < labels.length; i += xStep) {
            const x = pad.left + (i / (labels.length - 1)) * plotW;
            ctx.fillText(labels[i] + 'm', x, h - pad.bottom + 20);
        }

        // Lines
        series.forEach(s => {
            ctx.strokeStyle = s.color;
            ctx.lineWidth = 2;
            if (s.dash) ctx.setLineDash([6, 3]);
            else ctx.setLineDash([]);
            ctx.beginPath();
            s.data.forEach((v, i) => {
                const x = pad.left + (i / (labels.length - 1)) * plotW;
                const y = pad.top + plotH - (v / maxY) * plotH;
                if (i === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            });
            ctx.stroke();
        });
        ctx.setLineDash([]);

        // Legend
        const legendY = h - 8;
        let lx = pad.left;
        ctx.font = '11px Segoe UI, system-ui, sans-serif';
        series.forEach(s => {
            ctx.fillStyle = s.color;
            ctx.fillRect(lx, legendY - 8, 12, 3);
            ctx.fillStyle = c.textSec;
            ctx.textAlign = 'left';
            ctx.fillText(s.label, lx + 16, legendY - 3);
            lx += ctx.measureText(s.label).width + 30;
        });

        // Tooltip on hover
        this._addTooltip(id, labels, series, pad, plotW, plotH, maxY, c);
    },

    _addTooltip(id, labels, series, pad, plotW, plotH, maxY, c) {
        const canvas = document.getElementById(id);
        if (!canvas || canvas._hasTooltip) return;
        canvas._hasTooltip = true;
        canvas._tooltipData = { labels, series, pad, plotW, plotH, maxY };

        canvas.addEventListener('mousemove', (e) => {
            const rect = canvas.getBoundingClientRect();
            const mx = e.clientX - rect.left;
            const d = canvas._tooltipData;
            const idx = Math.round(((mx - d.pad.left) / d.plotW) * (d.labels.length - 1));
            if (idx < 0 || idx >= d.labels.length) { this._hideTooltip(); return; }

            let lines = [`${d.labels[idx]}m`];
            let items = d.series.filter(s => s.data[idx] !== undefined).map(s => ({name: s.label, val: s.data[idx], color: s.color}));
            items.sort((a,b) => b.val - a.val);
            items.forEach(it => lines.push(`${it.name}: ${it.val >= 1000 ? (it.val/1000).toFixed(1)+'k' : it.val}`));

            this._showTooltip(canvas, e.clientX, e.clientY, lines);
        });
        canvas.addEventListener('mouseleave', () => this._hideTooltip());
    },

    _showTooltip(canvas, x, y, lines) {
        let tip = document.getElementById('chart-tooltip');
        if (!tip) {
            tip = document.createElement('div');
            tip.id = 'chart-tooltip';
            tip.style.cssText = 'position:fixed;padding:8px 12px;background:#2A2A38;color:#EFDEBF;border:1px solid rgba(239,222,191,0.24);border-radius:6px;font:12px Consolas,monospace;pointer-events:none;z-index:9999;white-space:pre;';
            document.body.appendChild(tip);
        }
        tip.textContent = lines.join('\n');
        tip.style.left = (x + 15) + 'px';
        tip.style.top = (y - 10) + 'px';
        tip.style.display = 'block';
    },

    _hideTooltip() {
        const tip = document.getElementById('chart-tooltip');
        if (tip) tip.style.display = 'none';
    },

    renderAdvantageChart(id, labels, data) {
        const setup = this._setupCanvas(id, 250);
        if (!setup) return;
        const { ctx, w, h } = setup;
        const c = this._colors;
        const pad = { top: 20, right: 20, bottom: 40, left: 65 };
        const plotW = w - pad.left - pad.right;
        const plotH = h - pad.top - pad.bottom;

        ctx.fillStyle = c.cardBg;
        ctx.fillRect(0, 0, w, h);

        if (!data.length) return;

        let maxAbs = Math.max(...data.map(Math.abs));
        if (maxAbs === 0) maxAbs = 1;

        const zeroY = pad.top + plotH / 2;

        // Grid
        ctx.strokeStyle = c.grid;
        ctx.lineWidth = 1;
        ctx.font = '12px Consolas, Courier New, monospace';
        ctx.fillStyle = c.textSec;
        ctx.textAlign = 'right';
        [1, 0.5, 0, -0.5, -1].forEach(frac => {
            const y = zeroY - frac * (plotH / 2);
            ctx.beginPath();
            ctx.moveTo(pad.left, y);
            ctx.lineTo(w - pad.right, y);
            ctx.stroke();
            const val = Math.round(frac * maxAbs);
            ctx.fillText(val >= 1000 ? (val/1000).toFixed(1)+'k' : val, pad.left - 8, y + 4);
        });

        // Zero line
        ctx.strokeStyle = c.textSec;
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(pad.left, zeroY);
        ctx.lineTo(w - pad.right, zeroY);
        ctx.stroke();

        // X labels
        ctx.textAlign = 'center';
        ctx.fillStyle = c.textSec;
        const xStep = Math.max(1, Math.floor(labels.length / 10));
        for (let i = 0; i < labels.length; i += xStep) {
            const x = pad.left + (i / (labels.length - 1)) * plotW;
            ctx.fillText(labels[i] + 'm', x, h - pad.bottom + 20);
        }

        // Amber label top, Sapphire label bottom
        ctx.font = '11px Segoe UI, system-ui, sans-serif';
        ctx.textAlign = 'left';
        ctx.fillStyle = c.amber;
        ctx.fillText('Amber advantage', pad.left + 5, pad.top + 14);
        ctx.fillStyle = c.sapphire;
        ctx.fillText('Sapphire advantage', pad.left + 5, h - pad.bottom - 5);

        // Area fill
        ctx.beginPath();
        data.forEach((v, i) => {
            const x = pad.left + (i / (data.length - 1)) * plotW;
            const y = zeroY - (v / maxAbs) * (plotH / 2);
            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        });
        // Close to zero line
        ctx.lineTo(pad.left + plotW, zeroY);
        ctx.lineTo(pad.left, zeroY);
        ctx.closePath();

        // Use gradient: amber above, sapphire below
        const grad = ctx.createLinearGradient(0, pad.top, 0, pad.top + plotH);
        grad.addColorStop(0, 'rgba(232,148,58,0.3)');
        grad.addColorStop(0.5, 'rgba(232,148,58,0.05)');
        grad.addColorStop(0.5, 'rgba(58,123,213,0.05)');
        grad.addColorStop(1, 'rgba(58,123,213,0.3)');
        ctx.fillStyle = grad;
        ctx.fill();

        // Line
        ctx.beginPath();
        data.forEach((v, i) => {
            const x = pad.left + (i / (data.length - 1)) * plotW;
            const y = zeroY - (v / maxAbs) * (plotH / 2);
            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        });
        ctx.strokeStyle = '#EFDEBF';
        ctx.lineWidth = 2;
        ctx.stroke();
    },

    renderBarChart(id, labels, data) {
        const setup = this._setupCanvas(id, 250);
        if (!setup) return;
        const { ctx, w, h } = setup;
        const c = this._colors;
        const pad = { top: 20, right: 20, bottom: 40, left: 65 };
        const plotW = w - pad.left - pad.right;
        const plotH = h - pad.top - pad.bottom;

        ctx.fillStyle = c.cardBg;
        ctx.fillRect(0, 0, w, h);

        if (!data.length) return;

        let maxY = Math.max(...data);
        if (maxY === 0) maxY = 1;

        // Grid
        ctx.strokeStyle = c.grid;
        ctx.lineWidth = 1;
        ctx.font = '12px Consolas, Courier New, monospace';
        ctx.fillStyle = c.textSec;
        ctx.textAlign = 'right';
        for (let i = 0; i <= 4; i++) {
            const y = pad.top + plotH - (i / 4) * plotH;
            ctx.beginPath();
            ctx.moveTo(pad.left, y);
            ctx.lineTo(w - pad.right, y);
            ctx.stroke();
            ctx.fillText(Math.round((i / 4) * maxY), pad.left - 8, y + 4);
        }

        // Bars
        const barW = Math.max(2, (plotW / data.length) * 0.7);
        const gap = plotW / data.length;
        ctx.fillStyle = c.red;
        data.forEach((v, i) => {
            const x = pad.left + i * gap + (gap - barW) / 2;
            const barH = (v / maxY) * plotH;
            const y = pad.top + plotH - barH;
            ctx.fillRect(x, y, barW, barH);
        });

        // X labels
        ctx.textAlign = 'center';
        ctx.fillStyle = c.textSec;
        const xStep = Math.max(1, Math.floor(labels.length / 10));
        for (let i = 0; i < labels.length; i += xStep) {
            const x = pad.left + i * gap + gap / 2;
            ctx.fillText(labels[i] + 'm', x, h - pad.bottom + 20);
        }
    },

    renderPlayerKdChart(id, kills, deaths) {
        const setup = this._setupCanvas(id, 300);
        if (!setup) return;
        const { ctx, w, h } = setup;
        const c = this._colors;
        const pad = { top: 20, right: 20, bottom: 40, left: 50 };
        const plotW = w - pad.left - pad.right;
        const plotH = h - pad.top - pad.bottom;

        ctx.fillStyle = c.cardBg;
        ctx.fillRect(0, 0, w, h);

        let maxX = 0, maxY = 0;
        kills.forEach(p => { if (p.x > maxX) maxX = p.x; if (p.y > maxY) maxY = p.y; });
        deaths.forEach(p => { if (p.x > maxX) maxX = p.x; if (p.y > maxY) maxY = p.y; });
        if (maxX === 0) maxX = 1;
        if (maxY === 0) maxY = 1;

        // Grid
        ctx.strokeStyle = c.grid;
        ctx.lineWidth = 1;
        ctx.font = '12px Consolas, Courier New, monospace';
        ctx.fillStyle = c.textSec;
        ctx.textAlign = 'right';
        for (let i = 0; i <= 4; i++) {
            const y = pad.top + plotH - (i / 4) * plotH;
            ctx.beginPath(); ctx.moveTo(pad.left, y); ctx.lineTo(w - pad.right, y); ctx.stroke();
            ctx.fillText(Math.round((i / 4) * maxY), pad.left - 8, y + 4);
        }
        ctx.textAlign = 'center';
        for (let i = 0; i <= 5; i++) {
            const x = pad.left + (i / 5) * plotW;
            ctx.fillText(Math.round((i / 5) * maxX) + 'm', x, h - pad.bottom + 20);
        }

        function drawLine(points, color) {
            if (points.length < 2) return;
            ctx.strokeStyle = color;
            ctx.lineWidth = 2.5;
            ctx.beginPath();
            points.forEach((p, i) => {
                const x = pad.left + (p.x / maxX) * plotW;
                const y = pad.top + plotH - (p.y / maxY) * plotH;
                if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
            });
            ctx.stroke();

            // Points
            points.forEach(p => {
                const x = pad.left + (p.x / maxX) * plotW;
                const y = pad.top + plotH - (p.y / maxY) * plotH;
                ctx.beginPath();
                ctx.arc(x, y, 3, 0, Math.PI * 2);
                ctx.fillStyle = color;
                ctx.fill();
            });
        }

        drawLine(kills, c.green);
        drawLine(deaths, c.red);

        // Legend
        ctx.font = '12px Segoe UI, system-ui, sans-serif';
        ctx.fillStyle = c.green;
        ctx.fillRect(pad.left, h - 12, 12, 3);
        ctx.fillStyle = c.textSec;
        ctx.textAlign = 'left';
        ctx.fillText('Kills', pad.left + 16, h - 7);
        ctx.fillStyle = c.red;
        ctx.fillRect(pad.left + 70, h - 12, 12, 3);
        ctx.fillStyle = c.textSec;
        ctx.fillText('Deaths', pad.left + 86, h - 7);

        // Tooltip on hover
        const canvas = document.getElementById(id);
        if (canvas && !canvas._hasKdTooltip) {
            canvas._hasKdTooltip = true;
            canvas.addEventListener('mousemove', (e) => {
                const rect = canvas.getBoundingClientRect();
                const mx = e.clientX - rect.left;
                const my = e.clientY - rect.top;

                let closest = null, minDist = 20;
                const check = (points, type) => {
                    points.forEach(p => {
                        const x = pad.left + (p.x / maxX) * plotW;
                        const y = pad.top + plotH - (p.y / maxY) * plotH;
                        const d = Math.sqrt((mx-x)**2 + (my-y)**2);
                        if (d < minDist) { minDist = d; closest = { ...p, type }; }
                    });
                };
                check(kills, 'Kill');
                check(deaths, 'Death');

                if (closest && closest.tip) {
                    deadlockCharts._showTooltip(canvas, e.clientX, e.clientY, [closest.tip]);
                } else {
                    deadlockCharts._hideTooltip();
                }
            });
            canvas.addEventListener('mouseleave', () => deadlockCharts._hideTooltip());
        }
    }
};
