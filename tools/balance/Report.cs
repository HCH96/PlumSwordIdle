using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PlumSword.BalanceSim
{
    /// <summary>결과를 data.json 과 그래프 보고서 report.html 로 쓴다. HTML 은 데이터를 품은 파일 하나다.</summary>
    public static class Report
    {
        public const double TargetMinutes = 20;
        public const int TargetRegressions = 3;

        public static void Write(string htmlPath, string jsonPath, List<RunResult> runs, int seeds, double minutes)
        {
            var data = new
            {
                meta = new
                {
                    seeds,
                    minutes,
                    targetMinutes = TargetMinutes,
                    targetRegress = TargetRegressions,
                    generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                },
                runs = runs.Select(r => new
                {
                    name = r.Name,
                    changed = r.Changed,
                    table = r.Table.Select(kv => new[] { kv.Key, kv.Value }),
                    seeds = r.Seeds.Select(s => new
                    {
                        seed = s.Seed,
                        clear = s.ClearTime,
                        regress = s.Regressions,
                        fails = s.Fails,
                        end = s.EndTime,
                        s = new
                        {
                            t = s.Samples.Select(x => Math.Round(x.T / 60.0, 4)),
                            p = s.Samples.Select(x => (x.Disp - 1) * 7 + x.Sub),
                            gold = s.Samples.Select(x => x.Gold),
                            earn = s.Samples.Select(x => x.GoldEarned),
                            atkLv = s.Samples.Select(x => x.AtkLv),
                            hpLv = s.Samples.Select(x => x.HpLv),
                            innLv = s.Samples.Select(x => x.InnLv),
                            attack = s.Samples.Select(x => x.Attack),
                            maxHp = s.Samples.Select(x => x.MaxHp),
                            skill = s.Samples.Select(x => x.SkillDmg),
                            realm = s.Samples.Select(x => x.Realm),
                            eHp = s.Samples.Select(x => x.EnemyHp),
                            eAtk = s.Samples.Select(x => x.EnemyAtk),
                            sub = s.Samples.Select(x => x.Sub),
                        },
                        events = s.Events.Select(e => new { t = e.T, type = e.Type, disp = e.Disp, label = e.Label, detail = e.Detail }),
                        attempts = s.Attempts.Select(a => new { disp = a.Disp, start = a.Start, end = a.End, result = a.Result }),
                    }),
                }),
            };
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(jsonPath, json, new UTF8Encoding(false));
            File.WriteAllText(htmlPath, Html.Replace("/*DATA*/null", json), new UTF8Encoding(false));
        }

        private const string Html = """
<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>밸런스 시뮬레이션</title>
<script src="https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js"></script>
<style>
  :root { --bg:#faf7f8; --card:#fff; --ink:#1f1a1d; --mute:#6b6268; --line:#e7dfe3; --good:#15803d; --bad:#b91c1c; --warn:#b45309; }
  @media (prefers-color-scheme: dark) {
    :root { --bg:#151214; --card:#1f1a1d; --ink:#f2ecef; --mute:#a59aa0; --line:#352d31; --good:#4ade80; --bad:#f87171; --warn:#fbbf24; }
  }
  * { box-sizing: border-box; }
  body { margin:0; background:var(--bg); color:var(--ink); font:14px/1.5 system-ui, "Malgun Gothic", sans-serif; }
  main { max-width:1200px; margin:0 auto; padding:20px 16px 60px; }
  h1 { font-size:20px; margin:0 0 4px; }
  h2 { font-size:16px; margin:28px 0 8px; }
  .mute { color:var(--mute); }
  .cards { display:grid; grid-template-columns:repeat(auto-fit, minmax(260px, 1fr)); gap:12px; margin-top:12px; }
  .card { background:var(--card); border:1px solid var(--line); border-radius:10px; padding:12px 14px; }
  .card h3 { margin:0 0 6px; font-size:15px; display:flex; align-items:center; gap:8px; }
  .swatch { width:12px; height:12px; border-radius:3px; display:inline-block; }
  .big { font-size:22px; font-weight:600; }
  .good { color:var(--good); } .bad { color:var(--bad); } .warn { color:var(--warn); }
  .controls { position:sticky; top:0; z-index:5; background:var(--bg); padding:10px 0; border-bottom:1px solid var(--line); margin-top:16px; display:flex; gap:16px; flex-wrap:wrap; align-items:center; }
  select { font:inherit; padding:4px 8px; border-radius:6px; border:1px solid var(--line); background:var(--card); color:var(--ink); }
  .grid { display:grid; grid-template-columns:repeat(auto-fit, minmax(520px, 1fr)); gap:12px; }
  @media (max-width:600px) { .grid { grid-template-columns:1fr; } }
  .chart { background:var(--card); border:1px solid var(--line); border-radius:10px; padding:10px 12px; }
  .chart h3 { margin:0; font-size:14px; }
  .chart p { margin:2px 0 6px; font-size:12px; color:var(--mute); }
  .chart .box { position:relative; height:280px; }
  table { border-collapse:collapse; width:100%; background:var(--card); font-variant-numeric:tabular-nums; }
  th, td { border:1px solid var(--line); padding:4px 8px; text-align:right; white-space:nowrap; }
  th:first-child, td:first-child { text-align:left; }
  .scroll { overflow-x:auto; }
  details { margin-top:8px; }
  summary { cursor:pointer; color:var(--mute); }
  ul.changed { margin:4px 0 0; padding-left:18px; font-size:13px; }
</style>
</head>
<body>
<main>
  <h1>밸런스 시뮬레이션</h1>
  <div class="mute" id="meta"></div>
  <div class="cards" id="cards"></div>

  <div class="controls">
    <label>seed <select id="seed"></select></label>
    <span class="mute">그래프는 고른 seed 하나를 설정끼리 겹쳐 보여 준다. 표는 모든 seed.</span>
  </div>

  <div class="grid">
    <div class="chart"><h3>진행도</h3><p>세로축은 스테이지-구간. ✕ 회귀, ★ 천마 처치, ▲ 경지 돌파. 회색 점선은 목표 시간.</p><div class="box"><canvas id="cProg"></canvas></div></div>
    <div class="chart"><h3>전투 여유 (로그 축)</h3><p>실선: 상대 하나를 쓰러뜨리는 데 드는 기본 공격 수 = 상대 체력 ÷ 공격력. 점선: 제자가 버티는 상대 공격 수 = 최대 체력 ÷ 상대 공격력. 실선이 점선보다 위면 위험.</p><div class="box"><canvas id="cRatio"></canvas></div></div>
    <div class="chart"><h3>금화</h3><p>실선: 보유 금화. 점선: 누적 획득 금화 (기울기 = 버는 속도).</p><div class="box"><canvas id="cGold"></canvas></div></div>
    <div class="chart"><h3>강화 단계</h3><p>실선 무력, 점선 체력, 짧은 점선 내공.</p><div class="box"><canvas id="cLv"></canvas></div></div>
    <div class="chart"><h3>능력치 vs 상대 (로그 축)</h3><p>실선: 제자 공격력·최대 체력. 점선: 지금 상대의 체력·공격력.</p><div class="box"><canvas id="cStat"></canvas></div></div>
    <div class="chart"><h3>경지</h3><p>0 삼류 · 1 이류 · 2 일류 · 3 절정 · 4 초절정</p><div class="box"><canvas id="cRealm"></canvas></div></div>
  </div>

  <h2>스테이지를 처음 완료한 시각 (모든 seed)</h2>
  <div class="scroll"><table id="tFirst"></table></div>

  <h2>큰 스테이지 시도 (고른 seed)</h2>
  <p class="mute">시작부터 성공·패배까지 걸린 시간. 회귀마다 1스테이지부터 다시 쌓인다.</p>
  <div class="scroll" id="attempts"></div>

  <h2>수치 표</h2>
  <div id="tables"></div>
</main>

<script>
const DATA = /*DATA*/null;
const COLORS = ['#d9467a', '#2563eb', '#16a34a', '#d97706', '#7c3aed', '#0891b2'];
const REALMS = ['삼류', '이류', '일류', '절정', '초절정'];
const ink = getComputedStyle(document.documentElement).getPropertyValue('--ink').trim();
const mute = getComputedStyle(document.documentElement).getPropertyValue('--mute').trim();
const lineCol = getComputedStyle(document.documentElement).getPropertyValue('--line').trim();
Chart.defaults.color = mute;
Chart.defaults.borderColor = lineCol;
Chart.defaults.font.family = 'system-ui, "Malgun Gothic", sans-serif';

const clock = s => s == null ? '—' : `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(Math.floor(s) % 60).padStart(2, '0')}`;
const progLabel = v => { const d = Math.floor((v - 1) / 7) + 1, s = (v - 1) % 7 + 1; return d === 5 ? 'FINAL' : `${d}-${s}`; };
const median = a => { const b = [...a].sort((x, y) => x - y); const n = b.length; return n ? (n % 2 ? b[(n - 1) / 2] : (b[n / 2 - 1] + b[n / 2]) / 2) : null; };
const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

const M = DATA.meta;
document.getElementById('meta').textContent =
  `${M.generated} · seed ${M.seeds}개 · 최대 ${M.minutes}분 · 목표: 자동 강화로 약 ${M.targetMinutes}분, 회귀 ${M.targetRegress}회쯤에 천마 첫 처치`;

// 요약 카드
const cards = document.getElementById('cards');
DATA.runs.forEach((run, i) => {
  const times = run.seeds.filter(s => s.clear != null).map(s => s.clear);
  const med = median(times);
  const reg = run.seeds.reduce((a, s) => a + s.regress, 0) / run.seeds.length;
  const timeCls = med == null ? 'bad' : Math.abs(med / 60 - M.targetMinutes) <= 3 ? 'good' : 'warn';
  const regCls = Math.abs(reg - M.targetRegress) <= 1 ? 'good' : 'warn';
  const el = document.createElement('div');
  el.className = 'card';
  el.innerHTML = `<h3><span class="swatch" style="background:${COLORS[i % COLORS.length]}"></span>${esc(run.name)}</h3>
    <div>천마 첫 처치 <span class="big ${timeCls}">${med == null ? '못 잡음' : clock(med)}</span>
      <span class="mute">${times.length}/${run.seeds.length} seed${times.length ? ` · ${clock(Math.min(...times))}~${clock(Math.max(...times))}` : ''}</span></div>
    <div>평균 회귀 <span class="big ${regCls}">${reg.toFixed(1)}</span>회 <span class="mute">· 평균 패배 ${(run.seeds.reduce((a, s) => a + s.fails, 0) / run.seeds.length).toFixed(1)}회</span></div>
    ${run.changed.length ? `<details><summary>바꾼 값 ${run.changed.length}개</summary><ul class="changed">${run.changed.map(c => `<li>${esc(c)}</li>`).join('')}</ul></details>` : '<div class="mute">기본 표 그대로</div>'}`;
  cards.appendChild(el);
});

// seed 고르기
const seedSel = document.getElementById('seed');
DATA.runs[0].seeds.forEach((s, i) => { const o = document.createElement('option'); o.value = i; o.textContent = `seed ${s.seed}`; seedSel.appendChild(o); });

const charts = {};
function make(id, opts) {
  if (charts[id]) charts[id].destroy();
  charts[id] = new Chart(document.getElementById(id), {
    type: 'line',
    data: { datasets: opts.datasets },
    options: {
      animation: false, responsive: true, maintainAspectRatio: false, parsing: false, normalized: true,
      interaction: { mode: 'nearest', axis: 'x', intersect: false },
      plugins: {
        legend: { labels: { boxWidth: 12, filter: it => !it.text.startsWith('_') } },
        tooltip: { callbacks: { title: it => it.length ? `${clock(it[0].parsed.x * 60)}` : '', label: opts.label || (c => `${c.dataset.label}: ${c.parsed.y.toLocaleString()}`) } },
      },
      scales: {
        x: { type: 'linear', min: 0, title: { display: true, text: '분' } },
        y: Object.assign({ type: opts.log ? 'logarithmic' : 'linear' }, opts.y || {}),
      },
      elements: { point: { radius: 0 }, line: { borderWidth: 1.6 } },
    },
  });
}
const pts = (s, key, f) => s.t.map((t, i) => ({ x: t, y: f ? f(i) : s[key][i] }));
const dash = { solid: [], dash: [6, 4], dot: [2, 3] };

function render() {
  const si = +seedSel.value;
  const runs = DATA.runs.map((r, i) => ({ r, s: r.seeds[si], c: COLORS[i % COLORS.length] }));
  const maxProg = 35;

  // 진행도
  const prog = [];
  runs.forEach(({ r, s, c }) => {
    prog.push({ label: r.name, data: pts(s.s, 'p'), borderColor: c, stepped: true });
    const ev = (type, style, size) => s.events.filter(e => e.type === type).map(e => ({ x: e.t / 60, y: type === 'breakthrough' ? null : maxProg }));
    prog.push({ label: '_r', type: 'scatter', data: s.events.filter(e => e.type === 'regress').map(e => ({ x: e.t / 60, y: maxProg })), pointStyle: 'crossRot', pointRadius: 7, borderColor: c, borderWidth: 2 });
    prog.push({ label: '_c', type: 'scatter', data: s.events.filter(e => e.type === 'gameClear').map(e => ({ x: e.t / 60, y: maxProg })), pointStyle: 'star', pointRadius: 10, borderColor: c, backgroundColor: c, borderWidth: 2 });
    prog.push({ label: '_b', type: 'scatter', data: s.events.filter(e => e.type === 'breakthrough').map(e => {
      const i = s.s.t.findIndex(t => t >= e.t / 60); return { x: e.t / 60, y: s.s.p[Math.max(0, i)] };
    }), pointStyle: 'triangle', pointRadius: 5, backgroundColor: c, borderColor: c });
  });
  prog.push({ label: '_target', data: [{ x: M.targetMinutes, y: 0 }, { x: M.targetMinutes, y: maxProg }], borderColor: mute, borderDash: [4, 4], borderWidth: 1 });
  make('cProg', {
    datasets: prog,
    y: { min: 1, max: maxProg, ticks: { stepSize: 7, callback: v => v % 7 === 0 ? progLabel(v) : (v === 1 ? '1-1' : '') } },
    label: c => c.dataset.label.startsWith('_') ? (c.dataset.label === '_target' ? '목표' : '') : `${c.dataset.label}: ${progLabel(c.parsed.y)}`,
  });

  // 전투 여유
  const ratio = [];
  runs.forEach(({ r, s, c }) => {
    const S = s.s;
    ratio.push({ label: `${r.name} 쓰러뜨리는 공격 수`, data: pts(S, null, i => Math.max(1, Math.ceil(S.eHp[i] / Math.max(1, S.attack[i])))), borderColor: c, stepped: true });
    ratio.push({ label: `${r.name} 버티는 공격 수`, data: pts(S, null, i => Math.max(1, Math.ceil(S.maxHp[i] / Math.max(1, S.eAtk[i])))), borderColor: c, borderDash: dash.dash, stepped: true });
  });
  make('cRatio', { datasets: ratio, log: true });

  // 금화
  const gold = [];
  runs.forEach(({ r, s, c }) => {
    gold.push({ label: `${r.name} 보유`, data: pts(s.s, 'gold'), borderColor: c });
    gold.push({ label: `${r.name} 누적 획득`, data: pts(s.s, 'earn'), borderColor: c, borderDash: dash.dash });
  });
  make('cGold', { datasets: gold });

  // 강화 단계
  const lv = [];
  runs.forEach(({ r, s, c }) => {
    lv.push({ label: `${r.name} 무력`, data: pts(s.s, 'atkLv'), borderColor: c, stepped: true });
    lv.push({ label: `${r.name} 체력`, data: pts(s.s, 'hpLv'), borderColor: c, borderDash: dash.dash, stepped: true });
    lv.push({ label: `${r.name} 내공`, data: pts(s.s, 'innLv'), borderColor: c, borderDash: dash.dot, stepped: true });
  });
  make('cLv', { datasets: lv });

  // 능력치 vs 상대
  const st = [];
  runs.forEach(({ r, s, c }) => {
    st.push({ label: `${r.name} 공격력`, data: pts(s.s, 'attack'), borderColor: c, stepped: true });
    st.push({ label: `${r.name} 최대 체력`, data: pts(s.s, 'maxHp'), borderColor: c, borderWidth: 3, stepped: true });
    st.push({ label: `${r.name} 상대 체력`, data: pts(s.s, 'eHp'), borderColor: c, borderDash: dash.dash, borderWidth: 3, stepped: true });
    st.push({ label: `${r.name} 상대 공격력`, data: pts(s.s, 'eAtk'), borderColor: c, borderDash: dash.dash, stepped: true });
  });
  make('cStat', { datasets: st, log: true });

  // 경지
  make('cRealm', {
    datasets: runs.map(({ r, s, c }) => ({ label: r.name, data: pts(s.s, 'realm'), borderColor: c, stepped: true })),
    y: { min: 0, max: 4, ticks: { stepSize: 1, callback: v => REALMS[v] || '' } },
    label: c => `${c.dataset.label}: ${REALMS[c.parsed.y]}`,
  });

  // 시도 표
  const box = document.getElementById('attempts');
  box.innerHTML = runs.map(({ r, s, c }) => {
    const rows = s.attempts.map((a, i) => `<tr><td>${i + 1}</td><td>${a.disp === 5 ? 'FINAL (천마)' : a.disp + '스테이지'}</td><td>${clock(a.start)}</td><td>${clock(a.end - a.start)}</td>
      <td class="${a.result === 'clear' ? 'good' : a.result === 'timeout' ? 'mute' : 'bad'}">${{ clear: '성공', fail: '패배', regress: '패배 → 회귀', timeout: '시간 끝' }[a.result] || a.result}</td></tr>`).join('');
    return `<details ${runs.length === 1 ? 'open' : ''}><summary><span class="swatch" style="background:${c}"></span> ${esc(r.name)} · seed ${s.seed} · 시도 ${s.attempts.length}번</summary>
      <table><tr><th>#</th><th>스테이지</th><th>시작</th><th>걸린 시간</th><th>결과</th></tr>${rows}</table></details>`;
  }).join('');
}

// 처음 완료 표 (모든 seed)
(function () {
  let html = '<tr><th>설정</th><th>seed</th><th>1스테이지</th><th>2</th><th>3</th><th>4</th><th>천마</th><th>회귀</th><th>패배</th></tr>';
  DATA.runs.forEach((run, i) => {
    run.seeds.forEach(s => {
      const first = d => { const e = s.events.find(e => (e.type === 'clear' && e.disp === d)); return e ? e.t : null; };
      html += `<tr><td><span class="swatch" style="background:${COLORS[i % COLORS.length]}"></span> ${esc(run.name)}</td><td>${s.seed}</td>
        ${[1, 2, 3, 4, 5].map(d => `<td>${clock(first(d))}</td>`).join('')}<td>${s.regress}</td><td>${s.fails}</td></tr>`;
    });
  });
  document.getElementById('tFirst').innerHTML = html;
})();

// 수치 표
document.getElementById('tables').innerHTML = (function () {
  const keys = DATA.runs[0].table.map(kv => kv[0]);
  let h = '<div class="scroll"><table><tr><th>항목</th>' + DATA.runs.map((r, i) => `<th><span class="swatch" style="background:${COLORS[i % COLORS.length]}"></span> ${esc(r.name)}</th>`).join('') + '</tr>';
  keys.forEach((k, ki) => {
    const vals = DATA.runs.map(r => r.table[ki][1]);
    const diff = vals.some(v => v !== vals[0]);
    h += `<tr><td>${esc(k)}</td>${vals.map(v => `<td style="${diff ? 'font-weight:600' : ''}">${esc(v)}</td>`).join('')}</tr>`;
  });
  return h + '</table></div><p class="mute">굵은 값은 설정마다 다른 값. 곡선은 (base, linear, growth).</p>';
})();

seedSel.addEventListener('change', render);
render();
</script>
</body>
</html>
""";
    }
}
