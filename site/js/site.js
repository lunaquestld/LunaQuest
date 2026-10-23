// Animations du site : ciel étoilé, curseur, glitch du titre, cartes inclinables, aperçu, easter egg.
  (function () {
    var reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var fine = window.matchMedia("(pointer: fine)").matches;
    var mouse = { x: -999, y: -999 };

    // ---------- Ciel : étoiles, feuilles d'érable qui tombent, étincelles au clic ----------
    var canvas = document.getElementById("sky"), ctx = canvas.getContext("2d");
    var w = 0, h = 0, dpr = Math.min(window.devicePixelRatio || 1, 2), stars = [], leaves = [], sparks = [];
    var LEAF = [[0,-1],[.12,-.62],[.38,-.78],[.3,-.38],[.72,-.5],[.6,-.22],[.95,-.1],[.55,.12],[.62,.36],[.2,.25],[.08,.62],[0,.42],[-.08,.62],[-.2,.25],[-.62,.36],[-.55,.12],[-.95,-.1],[-.6,-.22],[-.72,-.5],[-.3,-.38],[-.38,-.78],[-.12,-.62]];
    function newLeaf(top) {
      var blue = Math.random() < .15;
      return { x: Math.random() * w, y: top ? -30 : Math.random() * h, s: 7 + Math.random() * 13, r: Math.random() * 6.28, vr: (Math.random() - .5) * .03,
        vy: .35 + Math.random() * .7, sway: Math.random() * 6.28, vx: 0, a: .35 + Math.random() * .45, c: blue ? "45,91,255" : "255,31,46" };
    }
    function resize() {
      w = window.innerWidth; h = window.innerHeight;
      canvas.width = w * dpr; canvas.height = h * dpr; ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      stars = [];
      for (var i = 0, n = Math.round(w * h / 5000); i < n; i++) {
        var k = Math.random();
        stars.push({ x: Math.random() * w, y: Math.random() * h, s: .4 + Math.pow(Math.random(), 3) * 1.7, p: Math.random() * 6.28, v: .5 + Math.random(),
          c: k < .1 ? "255,123,134" : k < .35 ? "143,178,255" : "244,243,255" });
      }
      leaves = [];
      for (var j = 0, m = Math.max(10, Math.round(w / 70)); j < m; j++) leaves.push(newLeaf(false));
      if (reduce) frame(0);
    }
    function drawLeaf(l) {
      ctx.save(); ctx.translate(l.x, l.y); ctx.rotate(l.r); ctx.scale(l.s, l.s);
      ctx.beginPath(); ctx.moveTo(LEAF[0][0], LEAF[0][1]);
      for (var i = 1; i < LEAF.length; i++) ctx.lineTo(LEAF[i][0], LEAF[i][1]);
      ctx.closePath();
      ctx.fillStyle = "rgba(" + l.c + "," + l.a + ")"; ctx.fill();
      ctx.lineWidth = .08; ctx.strokeStyle = "rgba(" + l.c + "," + l.a + ")";
      ctx.beginPath(); ctx.moveTo(0, .42); ctx.lineTo(0, 1.05); ctx.stroke();
      ctx.restore();
    }
    function frame(t) {
      ctx.clearRect(0, 0, w, h);
      for (var i = 0; i < stars.length; i++) {
        var s = stars[i], a = reduce ? .7 : .4 + .6 * Math.abs(Math.sin(s.p + t * .001 * s.v));
        ctx.fillStyle = "rgba(" + s.c + "," + a.toFixed(2) + ")";
        ctx.beginPath(); ctx.arc(s.x, s.y, s.s, 0, 6.2832); ctx.fill();
      }
      for (var j = 0; j < leaves.length; j++) {
        var l = leaves[j];
        if (!reduce) {
          // Les feuilles s'écartent du curseur.
          var dx = l.x - mouse.x, dy = l.y - mouse.y, d2 = dx * dx + dy * dy;
          if (d2 < 16000) { var f = (1 - d2 / 16000) * 2.4, d = Math.sqrt(d2) || 1; l.vx += dx / d * f; l.y += dy / d * f * .6; l.vr += .004; }
          l.vx *= .93; l.sway += .015;
          l.x += Math.sin(l.sway) * .6 + l.vx; l.y += l.vy; l.r += l.vr;
          if (l.y > h + 30 || l.x < -40 || l.x > w + 40) leaves[j] = newLeaf(true);
        }
        drawLeaf(l);
      }
      for (var k = sparks.length - 1; k >= 0; k--) {
        var p = sparks[k];
        p.x += p.vx; p.y += p.vy; p.vy += .12; p.vx *= .97; p.life -= .025;
        if (p.life <= 0) { sparks.splice(k, 1); continue; }
        ctx.strokeStyle = "rgba(" + p.c + "," + p.life.toFixed(2) + ")"; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(p.x, p.y); ctx.lineTo(p.x - p.vx * 3, p.y - p.vy * 3); ctx.stroke();
      }
    }
    function loop(t) { frame(t); requestAnimationFrame(loop); }
    window.addEventListener("resize", resize);
    resize();
    if (!reduce) requestAnimationFrame(loop);

    window.addEventListener("pointerdown", function (e) {
      if (reduce) return;
      for (var i = 0; i < 26; i++) {
        var ang = Math.random() * 6.28, sp = 2 + Math.random() * 6;
        sparks.push({ x: e.clientX, y: e.clientY, vx: Math.cos(ang) * sp, vy: Math.sin(ang) * sp - 2, life: 1, c: i % 3 === 0 ? "45,91,255" : i % 3 === 1 ? "255,31,46" : "207,224,255" });
      }
    });

    // ---------- Curseur ----------
    var cursor = document.getElementById("cursor"), dot = document.getElementById("cursor-dot");
    var cx = -100, cy = -100;
    if (fine && !reduce) {
      document.body.classList.add("fx-cursor");
      (function follow() {
        cx += (mouse.x - cx) * .2; cy += (mouse.y - cy) * .2;
        cursor.style.transform = "translate(" + cx + "px," + cy + "px)";
        requestAnimationFrame(follow);
      })();
    }
    window.addEventListener("pointermove", function (e) {
      mouse.x = e.clientX; mouse.y = e.clientY;
      dot.style.transform = "translate(" + e.clientX + "px," + e.clientY + "px)";
      var hot = e.target.closest && e.target.closest("a, button, summary, .btn, .tilt, .creator, .tabs button");
      cursor.classList.toggle("on", !!hot);
    });
    document.addEventListener("pointerleave", function () { mouse.x = mouse.y = -999; });

    // ---------- Barre de progression ----------
    var progress = document.getElementById("progress");
    function onScroll() {
      var max = document.documentElement.scrollHeight - window.innerHeight;
      progress.style.transform = "scaleX(" + (max > 0 ? window.scrollY / max : 0) + ")";
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    // ---------- Titre : glitch rouge/bleu et coup de katana ----------
    var titleWrap = document.getElementById("title"), h1 = document.getElementById("titre");
    function glitch() {
      var n = 0;
      var id = setInterval(function () {
        h1.style.setProperty("--gx1", (Math.random() * 14 - 7) + "px");
        h1.style.setProperty("--gx2", (Math.random() * 14 - 7) + "px");
        var a = Math.random() * 80, b = Math.random() * 80;
        h1.style.setProperty("--gc1", a + "%"); h1.style.setProperty("--gc2", (100 - a - 12) + "%");
        h1.style.setProperty("--gc3", b + "%"); h1.style.setProperty("--gc4", (100 - b - 10) + "%");
        if (++n > 5) { clearInterval(id); ["--gc1", "--gc3"].forEach(function (v) { h1.style.setProperty(v, "100%"); }); }
      }, 55);
    }
    function cut() { titleWrap.classList.remove("cut"); void titleWrap.offsetWidth; titleWrap.classList.add("cut"); glitch(); }
    if (!reduce) {
      setTimeout(cut, 500);
      titleWrap.addEventListener("mouseenter", cut);
      (function randomGlitch() { setTimeout(function () { glitch(); randomGlitch(); }, 2500 + Math.random() * 3500); })();
    }

    // ---------- Gyrophares au survol du bouton Télécharger ----------
    var dl = document.getElementById("download");
    dl.addEventListener("mouseenter", function () { document.body.classList.add("siren"); });
    dl.addEventListener("mouseleave", function () { document.body.classList.remove("siren"); });
    dl.addEventListener("focus", function () { document.body.classList.add("siren"); });
    dl.addEventListener("blur", function () { document.body.classList.remove("siren"); });

    // ---------- Boutons magnétiques ----------
    if (fine && !reduce) Array.prototype.forEach.call(document.querySelectorAll(".magnet"), function (b) {
      b.addEventListener("mousemove", function (e) {
        var r = b.getBoundingClientRect();
        b.style.transform = "translate(" + ((e.clientX - r.left - r.width / 2) * .25) + "px," + ((e.clientY - r.top - r.height / 2) * .35) + "px) scale(1.05) rotate(-1deg)";
      });
      b.addEventListener("mouseleave", function () { b.style.transform = ""; });
    });

    // ---------- Aperçu du hero qui suit la souris ----------
    var stage = document.getElementById("stage"), shot = document.getElementById("shot");
    if (fine && !reduce) {
      stage.addEventListener("mousemove", function (e) {
        var r = stage.getBoundingClientRect(), x = (e.clientX - r.left) / r.width - .5, y = (e.clientY - r.top) / r.height - .5;
        shot.style.transform = "rotateY(" + (-10 + x * 18).toFixed(1) + "deg) rotateX(" + (5 - y * 12).toFixed(1) + "deg) scale(1.02)";
        shot.style.setProperty("--sheen", (x * 160) + "%");
      });
      stage.addEventListener("mouseleave", function () { shot.style.transform = ""; shot.style.setProperty("--sheen", "-60%"); });
    }

    // ---------- Cartes : inclinaison 3D et halo ----------
    Array.prototype.forEach.call(document.querySelectorAll(".tilt"), function (card) {
      var tiltable = !card.matches("details, .release") && fine && !reduce;
      card.addEventListener("mousemove", function (e) {
        var r = card.getBoundingClientRect(), x = e.clientX - r.left, y = e.clientY - r.top;
        card.style.setProperty("--mx", x + "px"); card.style.setProperty("--my", y + "px");
        if (tiltable) card.style.transform = "perspective(900px) rotateX(" + ((.5 - y / r.height) * 8).toFixed(2) + "deg) rotateY(" + ((x / r.width - .5) * 10).toFixed(2) + "deg) translateY(-4px)";
      });
      card.addEventListener("mouseleave", function () { card.style.transform = ""; });
    });

    // ---------- Portrait du créateur en parallaxe ----------
    var creator = document.getElementById("creator"), portrait = document.getElementById("portrait-img");
    creator.addEventListener("mousemove", function (e) {
      var r = creator.getBoundingClientRect();
      portrait.style.setProperty("--px", ((e.clientX - r.left) / Math.max(r.width, 1) - .5) * -14 + "px");
      portrait.style.setProperty("--py", ((e.clientY - r.top) / 300 - .5) * -14 + "px");
    });

    // ---------- Easter egg : au 3e survol (ou toucher) du créateur, la vidéo remplace le grand portrait ----------
    var portraitBox = document.getElementById("portrait"), eggVideo = document.getElementById("egg-video");
    var hits = 0, lastHit = 0;
    function stopEgg() { portraitBox.classList.remove("playing"); creator.classList.remove("egg"); }
    function hitCreator() {
      if (portraitBox.classList.contains("playing")) return;
      var now = Date.now();
      hits = now - lastHit > 6000 ? 1 : hits + 1;
      lastHit = now;
      if (hits >= 3) { hits = 0; playEgg(); }
    }
    function playEgg() {
      // La carte créateur reste ouverte le temps de la vidéo, même si la souris s'en va.
      portraitBox.classList.add("playing");
      creator.classList.add("egg");
      eggVideo.currentTime = 0;
      eggVideo.muted = false;
      // Le son n'est permis qu'après un clic sur la page : sinon la vidéo se joue en muet.
      eggVideo.play().catch(function () {
        eggVideo.muted = true;
        eggVideo.play().catch(stopEgg);
      });
    }
    eggVideo.addEventListener("ended", stopEgg);
    creator.addEventListener("mouseenter", hitCreator);
    creator.addEventListener("touchstart", hitCreator, { passive: true });

    // ---------- Apparition au défilement (le contenu reste visible sans script) ----------
    function countUp(el) {
      var to = +el.getAttribute("data-to"), start = null;
      if (reduce || to === 0) { el.textContent = to; return; }
      (function step(ts) {
        if (!start) start = ts;
        var k = Math.min(1, (ts - start) / 1100);
        el.textContent = Math.round(to * (1 - Math.pow(1 - k, 3)));
        if (k < 1) requestAnimationFrame(step);
      })(performance.now());
    }
    if ("IntersectionObserver" in window) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (en) {
          if (!en.isIntersecting) return;
          en.target.classList.add("seen");
          if (en.target.classList.contains("count")) countUp(en.target);
          io.unobserve(en.target);
        });
      }, { threshold: .35 });
      Array.prototype.forEach.call(document.querySelectorAll(".head, #bars, .count"), function (el) { io.observe(el); });
    } else {
      Array.prototype.forEach.call(document.querySelectorAll(".head, #bars"), function (el) { el.classList.add("seen"); });
    }

    // ---------- Onglets de l'aperçu, avec déchirure glitch ----------
    var tabs = document.querySelectorAll(".tabs button"), viewer = document.getElementById("viewer");
    var img = document.getElementById("preview-img"), cap = document.getElementById("preview-cap");
    Array.prototype.forEach.call(tabs, function (tab) {
      tab.addEventListener("click", function () {
        if (tab.getAttribute("aria-selected") === "true") return;
        Array.prototype.forEach.call(tabs, function (t) { t.setAttribute("aria-selected", t === tab ? "true" : "false"); });
        viewer.setAttribute("aria-labelledby", tab.id);
        img.src = tab.getAttribute("data-img");
        img.alt = "Écran " + tab.textContent + " de LunaQuest";
        cap.textContent = tab.getAttribute("data-cap");
        if (!reduce) { viewer.classList.remove("glitching"); void viewer.offsetWidth; viewer.classList.add("glitching"); }
      });
    });
  })();
