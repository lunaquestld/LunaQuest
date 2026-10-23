// Page de modération : réservée aux comptes marqués admin dans la base.
// La page elle-même n'est pas secrète : c'est la base qui refuse toute action à un non-admin.
(function () {
  "use strict";

  var cfg = window.LUNAQUEST_SUPABASE || {};
  var msg = document.getElementById("msg");
  function say(text, error) { msg.textContent = text || ""; msg.className = "msg" + (error ? " error" : ""); }
  function el(tag, cls, text) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    return e;
  }
  function button(label, cls, onClick) {
    var b = el("button", cls, label);
    b.type = "button";
    b.addEventListener("click", onClick);
    return b;
  }
  function date(iso) { return new Date(iso).toLocaleString("fr-FR"); }

  if (!cfg.url || !cfg.anonKey || !window.supabase) return say("Supabase n'est pas encore configuré (js/config.js).", true);

  var sb = window.supabase.createClient(cfg.url, cfg.anonKey, { auth: { persistSession: true, autoRefreshToken: true } });
  var login = document.getElementById("login"), panel = document.getElementById("panel");
  var content = document.getElementById("content"), more = document.getElementById("more"), logout = document.getElementById("logout");
  var tab = "hidden", oldest = null;
  var STATUS = { public: "Public", hidden: "Masqué", deleted: "Supprimé" };

  function rpc(name, args) {
    return sb.rpc(name, args || {}).then(function (r) {
      if (r.error) { say(r.error.message, true); throw r.error; }
      return r.data;
    });
  }

  // ---------------------------------------------------------------- Commentaires
  function renderComment(c) {
    var card = el("div", "card item");
    var meta = el("div", "meta");
    var who = el("span");
    who.appendChild(el("b", null, c.author_pseudo));
    if (c.anonymous) who.appendChild(document.createTextNode(" (publié en anonyme)"));
    if (c.author_banned) who.appendChild(document.createTextNode(" · compte bloqué"));
    meta.appendChild(who);
    meta.appendChild(el("span", null, date(c.created_at)));
    meta.appendChild(el("span", "tag " + c.status, STATUS[c.status] || c.status));
    if (c.flag_reason) meta.appendChild(el("span", null, "Raison : " + c.flag_reason));
    if (c.reports > 0) meta.appendChild(el("span", null, c.reports + " signalement(s)"));
    card.appendChild(meta);
    card.appendChild(el("p", "body", c.body));

    var actions = el("div", "actions");
    function setStatus(status) {
      return function () {
        rpc("admin_set_comment_status", { p_id: c.id, p_status: status }).then(function () {
          say("Commentaire : " + STATUS[status] + ".");
          if (tab !== "all") card.remove(); else load(true);
        });
      };
    }
    if (c.status !== "public") actions.appendChild(button("Rendre public", "ok", setStatus("public")));
    if (c.status !== "hidden") actions.appendChild(button("Masquer", "", setStatus("hidden")));
    if (c.status !== "deleted") actions.appendChild(button("Supprimer (archivé)", "danger", setStatus("deleted")));
    if (!c.author_banned) {
      actions.appendChild(button("Bloquer l'auteur", "danger", function () {
        var reason = prompt("Bloquer " + c.author_pseudo + " ? Ses commentaires publics seront masqués.\n\nRaison (facultatif) :", "spam");
        if (reason === null) return;
        rpc("admin_ban_user", { p_user: c.author_id, p_reason: reason, p_hide_comments: true }).then(function () {
          say(c.author_pseudo + " est bloqué.");
          load(true);
        });
      }));
    }
    card.appendChild(actions);
    return card;
  }

  function loadComments(reset) {
    rpc("admin_list_comments", { p_status: tab, p_limit: 50, p_before: oldest }).then(function (rows) {
      if (reset && !rows.length) content.appendChild(el("p", "msg", "Rien ici."));
      rows.forEach(function (c) { content.appendChild(renderComment(c)); oldest = c.id; });
      more.hidden = rows.length < 50;
    });
  }

  // ---------------------------------------------------------------- Comptes bloqués
  function loadBanned() {
    rpc("admin_list_banned").then(function (rows) {
      if (!rows.length) return content.appendChild(el("p", "msg", "Aucun compte bloqué."));
      rows.forEach(function (u) {
        var card = el("div", "card item");
        var meta = el("div", "meta");
        meta.appendChild(el("b", null, u.pseudo));
        meta.appendChild(el("span", null, "Bloqué le " + date(u.banned_at)));
        if (u.banned_reason) meta.appendChild(el("span", null, "Raison : " + u.banned_reason));
        card.appendChild(meta);
        var actions = el("div", "actions");
        actions.appendChild(button("Débloquer", "ok", function () {
          rpc("admin_unban_user", { p_user: u.id }).then(function () { say(u.pseudo + " est débloqué. Ses commentaires restent masqués : rends-les publics un par un si besoin."); card.remove(); });
        }));
        card.appendChild(actions);
        content.appendChild(card);
      });
    });
  }

  // ---------------------------------------------------------------- Mots filtrés
  function loadWords() {
    var card = el("div", "card");
    card.appendChild(el("p", "msg", "Les commentaires contenant ces mots (au pluriel aussi, même avec des chiffres à la place des lettres) sont masqués en attendant ta vérification."));
    var form = el("form", "row");
    var input = el("input");
    input.placeholder = "Mot ou expression à filtrer";
    input.maxLength = 40;
    form.appendChild(input);
    var add = el("button", null, "Ajouter");
    add.type = "submit";
    form.appendChild(add);
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      var w = input.value.trim();
      if (w.length < 2) return;
      rpc("admin_add_word", { p_word: w }).then(function () { load(true); });
    });
    card.appendChild(form);
    var box = el("div", "words");
    card.appendChild(box);
    content.appendChild(card);
    rpc("admin_list_words").then(function (words) {
      words.forEach(function (w) {
        var chip = el("span", null, w);
        chip.appendChild(button("×", "", function () {
          rpc("admin_remove_word", { p_word: w }).then(function () { chip.remove(); });
        }));
        box.appendChild(chip);
      });
    });
  }

  function load(reset) {
    if (reset) { content.textContent = ""; oldest = null; more.hidden = true; }
    if (tab === "banned") return loadBanned();
    if (tab === "words") return loadWords();
    loadComments(reset);
  }
  more.addEventListener("click", function () { load(false); });
  Array.prototype.forEach.call(document.querySelectorAll("[data-tab]"), function (b) {
    b.addEventListener("click", function () {
      Array.prototype.forEach.call(document.querySelectorAll("[data-tab]"), function (x) { x.setAttribute("aria-selected", x === b); });
      tab = b.getAttribute("data-tab");
      say("");
      load(true);
    });
  });

  // ---------------------------------------------------------------- Connexion
  function check(session) {
    if (!session) { login.hidden = false; panel.hidden = true; logout.hidden = true; return; }
    logout.hidden = false;
    sb.rpc("my_profile").then(function (r) {
      var p = r.data && r.data[0];
      if (!p || !p.is_admin) {
        login.hidden = true; panel.hidden = true;
        return say("Ce compte n'a pas accès à la modération.", true);
      }
      login.hidden = true; panel.hidden = false;
      say("");
      load(true);
    });
  }
  login.addEventListener("submit", function (e) {
    e.preventDefault();
    sb.auth.signInWithPassword({ email: login.email.value.trim(), password: login.password.value }).then(function (r) {
      if (r.error) return say("Connexion impossible : e-mail ou mot de passe incorrect.", true);
      login.reset();
    });
  });
  logout.addEventListener("click", function () { sb.auth.signOut(); });
  sb.auth.onAuthStateChange(function (event, session) {
    if (event === "SIGNED_IN" || event === "SIGNED_OUT" || event === "INITIAL_SESSION") setTimeout(function () { check(session); }, 0);
  });
})();
