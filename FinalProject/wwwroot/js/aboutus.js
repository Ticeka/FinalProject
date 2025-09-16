/* AboutUs — minimal interactivity
   - reveal on view
   - count-up for stats
   - testimonials auto rotate
*/
(() => {
    const $ = (s, r = document) => r.querySelector(s);
    const $$ = (s, r = document) => Array.from(r.querySelectorAll(s));

    // Reveal on view
    const reveals = $$('.reveal');
    if (revealIntersectionSupported()) {
        const io = new IntersectionObserver((ents) => {
            ents.forEach(e => {
                if (e.isIntersecting) {
                    e.target.classList.add('show');
                    io.unobserve(e.target);
                }
            });
        }, { threshold: 0.15 });
        reveals.forEach(el => io.observe(el));
    } else {
        reveals.forEach(el => el.classList.add('show'));
    }

    // Count-up
    const counters = $$('.count-up');
    let started = false;
    if (counters.length && revealIntersectionSupported()) {
        const io2 = new IntersectionObserver((ents) => {
            if (started) return;
            ents.forEach(e => {
                if (e.isIntersecting) {
                    started = true;
                    counters.forEach(el => animateCount(el));
                    io2.disconnect();
                }
            });
        }, { threshold: 0.25 });
        io2.observe(counters[0]);
    } else {
        counters.forEach(el => el.textContent = numFormat(getTarget(el)));
    }

    function animateCount(el) {
        const target = getTarget(el);
        const dur = 900;
        const start = performance.now();
        const nf = new Intl.NumberFormat('th-TH');
        const step = (t) => {
            const p = Math.min(1, (t - start) / dur);
            const eased = 1 - Math.pow(1 - p, 3);
            el.textContent = nf.format(Math.round(target * eased));
            if (p < 1) requestAnimationFrame(step);
        };
        requestAnimationFrame(step);
    }
    function getTarget(el) { return Number(el.dataset.target || el.textContent || 0); }
    function numFormat(n) { return new Intl.NumberFormat('th-TH').format(Number(n) || 0); }
    function revealIntersectionSupported() { return 'IntersectionObserver' in window; }

    // Testimonials rotate
    const wrap = $('#testiWrap');
    if (wrap) {
        const items = $$('.testi-card', wrap);
        if (items.length) {
            let i = items.findIndex(el => el.classList.contains('show'));
            if (i < 0) i = 0;
            const INTERVAL = 3600;
            let timer = null;

            const show = (idx) => {
                items.forEach((el, k) => el.classList.toggle('show', k === idx));
            };
            const start = () => {
                if (items.length < 2 || prefersReduced()) return;
                stop();
                timer = setInterval(() => { i = (i + 1) % items.length; show(i); }, INTERVAL);
            };
            const stop = () => { if (timer) { clearInterval(timer); timer = null; } };
            const prefersReduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

            show(i);
            start();
            wrap.addEventListener('pointerenter', stop);
            wrap.addEventListener('pointerleave', start);
        }
    }
})();
