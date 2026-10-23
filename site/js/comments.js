// Espace commentaires de LunaQuest : comptes (Supabase Auth) et commentaires.
// Tout le texte venant des visiteurs est affiché avec textContent, jamais interprété comme du HTML.
(function () {
  "use strict";

  var root = document.getElementById("avis");
  if (!root) return;
  var cfg = window.LUNAQUEST_SUPABASE || {};
  var dialog = document.getElementById("auth-dialog");

  function $(sel, scope) { return (scope || document).querySelector(sel); }
  function el(tag, cls, text) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    return e;
  }
  function show(node, on) { if (node) node.hidden = !on; }
  function say(node, text, kind) {
    node.textContent = text || "";
    node.className = "form-msg" + (kind ? " " + kind : "");
  }

  if (!cfg.url || !cfg.anonKey || !window.supabase) {
    show($("#avis-offline"), true);
    show($("#avis-main"), false);
    return;
  }

  var sb = window.supabase.createClient(cfg.url, cfg.anonKey, {
    auth: { persistSession: true, autoRefreshToken: true, detectSessionInUrl: true }
  });
  var here = location.origin + location.pathname;
  var me = null; // { pseudo, is_admin, banned }
  var oldest = null;
  var PAGE = 15;

  // ---------------------------------------------------------------- Messages d'erreur en français
  function frError(err) {
    var m = (err && (err.message || err.error_description)) || String(err || "");
    if (/invalid login credentials/i.test(m)) return "E-mail ou mot de passe incorrect.";
    if (/email not confirmed/i.test(m)) return "Confirme d'abord ton adresse avec le lien reçu par e-mail.";
    if (/already registered|already been registered/i.test(m)) return "Un compte existe déjà avec cette adresse.";
    if (/password should be|weak password|password is too/i.test(m)) return "Mot de passe trop faible : 8 caractères minimum, avec lettres et chiffres.";
    if (/rate limit|security purposes|too many/i.test(m)) return "Trop de tentatives. Réessaie dans une minute.";
    if (/captcha/i.test(m)) return "La vérification anti-robot a échoué. Recharge la page et réessaie.";
    if (/network|fetch/i.test(m)) return "Connexion impossible. Vérifie ta connexion internet.";
    if (/jwt|token/i.test(m)) return "Ta session a expiré. Reconnecte-toi.";
    return m || "Une erreur est survenue.";
  }

  // ---------------------------------------------------------------- Dates
  var rtf = window.Intl && Intl.RelativeTimeFormat ? new Intl.RelativeTimeFormat("fr", { numeric: "auto" }) : null;
  function when(iso) {
    var d = new Date(iso), s = (Date.now() - d.getTime()) / 1000;
    if (rtf && s < 60) return "à l'instant";
    if (rtf && s < 3600) return rtf.format(-Math.round(s / 60), "minute");
    if (rtf && s < 86400) return rtf.format(-Math.round(s / 3600), "hour");
    if (rtf && s < 604800) return rtf.format(-Math.round(s / 86400), "day");
    return d.toLocaleDateString("fr-FR", { day: "numeric", month: "long", year: "numeric" });
  }

  // ---------------------------------------------------------------- Liste des commentaires
  var list = $("#avis-list"), more = $("#avis-more"), empty = $("#avis-empty"), listMsg = $("#avis-list-msg");

  function renderComment(c) {
    var li = el("li", "comment tilt");
    var head = el("div", "comment-head");
    head.appendChild(el("b", c.author === "Anonyme" ? "comment-author anon" : "comment-author", c.author));
    if (c.is_creator) head.appendChild(el("span", "badge", "Créateur"));
    var time = el("time", "comment-date", when(c.created_at));
    time.dateTime = c.created_at;
    time.title = new Date(c.created_at).toLocaleString("fr-FR");
    head.appendChild(time);
    li.appendChild(head);
    li.appendChild(el("p", "comment-body", c.body));

    if (me) {
      var actions = el("div", "comment-actions");
      if (c.is_mine) {
        var del = el("button", "link-btn", "Supprimer");
        del.type = "button";
        del.addEventListener("click", function () {
          if (!confirm("Supprimer ton commentaire ? C'est définitif.")) return;
          sb.rpc("delete_my_comment", { p_id: c.id }).then(function (r) {
            if (r.error) return alert(frError(r.error));
            li.remove();
            if (!list.children.length) show(empty, true);
          });
        });
        actions.appendChild(del);
      } else if (!me.banned) {
        var rep = el("button", "link-btn", "Signaler");
        rep.type = "button";
        rep.addEventListener("click", function () {
          if (!confirm("Signaler ce commentaire au créateur ? Il sera masqué s'il est signalé par plusieurs personnes.")) return;
          sb.rpc("report_comment", { p_id: c.id }).then(function (r) {
            if (r.error) return alert(frError(r.error));
            rep.textContent = "Signalé, merci";
            rep.disabled = true;
          });
        });
        actions.appendChild(rep);
      }
      li.appendChild(actions);
    }
    return li;
  }

  function loadComments(reset) {
    if (reset) { list.textContent = ""; oldest = null; }
    say(listMsg, "Chargement…");
    more.disabled = true;
    sb.rpc("get_public_comments", { p_limit: PAGE, p_before: oldest }).then(function (r) {
      more.disabled = false;
      if (r.error) return say(listMsg, frError(r.error), "error");
      say(listMsg, "");
      (r.data || []).forEach(function (c) { list.appendChild(renderComment(c)); oldest = c.id; });
      show(empty, !list.children.length);
      show(more, (r.data || []).length === PAGE);
    });
  }
  more.addEventListener("click", function () { loadComments(false); });

  // ---------------------------------------------------------------- Formulaire de commentaire
  var text = $("#avis-text"), count = $("#avis-count"), anon = $("#avis-anon"), send = $("#avis-send"), postMsg = $("#avis-status");
  text.addEventListener("input", function () { count.textContent = text.value.length + " / 1000"; });

  $("#avis-form").addEventListener("submit", function (e) {
    e.preventDefault();
    var body = text.value.trim();
    if (body.length < 2) return say(postMsg, "Écris au moins quelques mots.", "error");
    send.disabled = true;
    say(postMsg, "Publication…");
    sb.rpc("post_comment", { p_body: body, p_anonymous: anon.checked }).then(function (r) {
      send.disabled = false;
      if (r.error) return say(postMsg, frError(r.error), "error");
      text.value = "";
      count.textContent = "0 / 1000";
      if (r.data === "public") {
        say(postMsg, "Merci, ton commentaire est publié !", "ok");
        loadComments(true);
      } else {
        say(postMsg, "Merci ! Ton commentaire contient un lien ou un mot filtré : il sera publié après vérification par le créateur.", "ok");
      }
    });
  });

  // ---------------------------------------------------------------- État connecté / déconnecté
  function refreshUser(session) {
    if (!session) {
      me = null;
      show($("#avis-guest"), true);
      show($("#avis-member"), false);
      loadComments(true);
      return;
    }
    sb.rpc("my_profile").then(function (r) {
      me = (r.data && r.data[0]) || { pseudo: "membre", is_admin: false, banned: false };
      show($("#avis-guest"), false);
      show($("#avis-member"), true);
      $("#avis-me").textContent = me.pseudo;
      $("#acc-pseudo-now").textContent = me.pseudo;
      show($("#avis-admin-link"), !!me.is_admin);
      show($("#avis-form"), !me.banned);
      show($("#avis-banned"), !!me.banned);
      loadComments(true);
    });
  }

  sb.auth.onAuthStateChange(function (event, session) {
    if (event === "PASSWORD_RECOVERY") { openDialog("recovery"); return; }
    if (event === "SIGNED_IN" || event === "SIGNED_OUT" || event === "INITIAL_SESSION" || event === "USER_UPDATED") {
      setTimeout(function () { refreshUser(session); }, 0);
    }
  });

  $("#avis-logout").addEventListener("click", function () { sb.auth.signOut(); });

  // ---------------------------------------------------------------- Fenêtre compte
  function openDialog(panel) {
    Array.prototype.forEach.call(dialog.querySelectorAll("[data-panel]"), function (p) { p.hidden = p.getAttribute("data-panel") !== panel; });
    Array.prototype.forEach.call(dialog.querySelectorAll(".form-msg"), function (m) { say(m, ""); });
    if (!dialog.open) dialog.showModal();
    var first = dialog.querySelector('[data-panel="' + panel + '"] input');
    if (first) first.focus();
  }
  Array.prototype.forEach.call(document.querySelectorAll("[data-open]"), function (b) {
    b.addEventListener("click", function () { openDialog(b.getAttribute("data-open")); });
  });
  Array.prototype.forEach.call(dialog.querySelectorAll("[data-close]"), function (b) {
    b.addEventListener("click", function () { dialog.close(); });
  });
  dialog.addEventListener("click", function (e) { if (e.target === dialog) dialog.close(); });

  function busy(form, on) {
    Array.prototype.forEach.call(form.querySelectorAll("button, input"), function (x) { x.disabled = on; });
  }

  $("#form-login").addEventListener("submit", function (e) {
    e.preventDefault();
    var f = e.target, msg = $(".form-msg", f);
    busy(f, true);
    sb.auth.signInWithPassword({ email: f.email.value.trim(), password: f.password.value }).then(function (r) {
      busy(f, false);
      if (r.error) return say(msg, frError(r.error), "error");
      f.reset();
      dialog.close();
    });
  });

  $("#form-signup").addEventListener("submit", function (e) {
    e.preventDefault();
    var f = e.target, msg = $(".form-msg", f);
    var pseudo = f.pseudo.value.trim();
    if (!/^[A-Za-z0-9_.\-]{2,20}$/.test(pseudo)) return say(msg, "Le pseudo doit faire 2 à 20 caractères : lettres, chiffres, point, tiret ou tiret bas.", "error");
    if (!/^\S+@\S+\.\S+$/.test(f.email.value.trim())) return say(msg, "Cette adresse e-mail n'est pas valide.", "error");
    if (f.password.value.length < 8) return say(msg, "Le mot de passe doit faire au moins 8 caractères.", "error");
    if (f.password.value !== f.password2.value) return say(msg, "Les deux mots de passe ne sont pas identiques.", "error");
    if (!f.terms.checked) return say(msg, "Accepte les règles et la politique de confidentialité pour continuer.", "error");
    busy(f, true);
    say(msg, "Vérification du pseudo…");
    sb.rpc("pseudo_available", { p_pseudo: pseudo }).then(function (r) {
      if (r.error || r.data) { busy(f, false); return say(msg, r.error ? frError(r.error) : r.data, "error"); }
      return sb.auth.signUp({
        email: f.email.value.trim(),
        password: f.password.value,
        options: { data: { pseudo: pseudo }, emailRedirectTo: here + "#avis" }
      }).then(function (s) {
        busy(f, false);
        if (s.error) return say(msg, frError(s.error), "error");
        f.reset();
        say(msg, "Presque fini ! Ouvre l'e-mail de confirmation qu'on vient de t'envoyer et clique sur le lien pour activer ton compte. Pas reçu d'ici quelques minutes ? Regarde dans tes spams.", "ok");
      });
    });
  });

  $("#form-forgot").addEventListener("submit", function (e) {
    e.preventDefault();
    var f = e.target, msg = $(".form-msg", f);
    busy(f, true);
    sb.auth.resetPasswordForEmail(f.email.value.trim(), { redirectTo: here + "#avis" }).then(function (r) {
      busy(f, false);
      if (r.error) return say(msg, frError(r.error), "error");
      say(msg, "Si un compte existe avec cette adresse, tu vas recevoir un lien pour choisir un nouveau mot de passe. Pense à regarder dans tes spams.", "ok");
    });
  });

  $("#form-recovery").addEventListener("submit", function (e) {
    e.preventDefault();
    var f = e.target, msg = $(".form-msg", f);
    if (f.password.value.length < 8) return say(msg, "Le mot de passe doit faire au moins 8 caractères.", "error");
    if (f.password.value !== f.password2.value) return say(msg, "Les deux mots de passe ne sont pas identiques.", "error");
    busy(f, true);
    sb.auth.updateUser({ password: f.password.value }).then(function (r) {
      busy(f, false);
      if (r.error) return say(msg, frError(r.error), "error");
      f.reset();
      say(msg, "Mot de passe changé. Tu es connecté.", "ok");
    });
  });

  $("#form-pseudo").addEventListener("submit", function (e) {
    e.preventDefault();
    var f = e.target, msg = $(".form-msg", f);
    busy(f, true);
    sb.rpc("set_my_pseudo", { p_pseudo: f.pseudo.value.trim() }).then(function (r) {
      busy(f, false);
      if (r.error) return say(msg, frError(r.error), "error");
      f.reset();
      say(msg, "Pseudo modifié.", "ok");
      sb.auth.getSession().then(function (s) { refreshUser(s.data.session); });
    });
  });

  $("#acc-delete").addEventListener("click", function () {
    var msg = $("#acc-delete-msg");
    if (!confirm("Supprimer ton compte et tous tes commentaires ? C'est définitif.")) return;
    sb.rpc("delete_my_account").then(function (r) {
      if (r.error) return say(msg, frError(r.error), "error");
      sb.auth.signOut().then(function () { dialog.close(); });
    });
  });
})();
