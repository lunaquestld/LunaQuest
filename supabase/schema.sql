-- =====================================================================================
-- LunaQuest : espace commentaires (Supabase)
--
-- À coller une seule fois dans Supabase → SQL Editor → New query → Run.
--
-- Principe de sécurité : les tables sont fermées (RLS activée, aucune règle d'accès,
-- aucun droit pour les visiteurs). Le site ne peut passer que par les fonctions
-- ci-dessous, qui vérifient elles-mêmes qui appelle et ce qui est autorisé.
-- =====================================================================================

create extension if not exists unaccent with schema extensions;

-- ------------------------------------------------------------------ Tables

create table if not exists public.profiles (
  id            uuid primary key references auth.users (id) on delete cascade,
  pseudo        text not null,
  is_admin      boolean not null default false,
  banned        boolean not null default false,
  banned_reason text,
  banned_at     timestamptz,
  created_at    timestamptz not null default now()
);
create unique index if not exists profiles_pseudo_lower on public.profiles (lower(pseudo));

create table if not exists public.comments (
  id           bigint generated always as identity primary key,
  user_id      uuid not null references public.profiles (id) on delete cascade,
  body         text not null check (char_length(body) between 1 and 1000),
  anonymous    boolean not null default false,
  -- public : visible par tous · hidden : masqué (filtre, signalements ou admin), récupérable
  -- deleted : supprimé par l'admin, gardé en archive
  status       text not null default 'public' check (status in ('public', 'hidden', 'deleted')),
  flag_reason  text,
  created_at   timestamptz not null default now(),
  moderated_at timestamptz
);
create index if not exists comments_status_id on public.comments (status, id desc);
create index if not exists comments_user_created on public.comments (user_id, created_at desc);

create table if not exists public.reports (
  comment_id bigint not null references public.comments (id) on delete cascade,
  user_id    uuid not null references public.profiles (id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (comment_id, user_id)
);

create table if not exists public.blocked_words (
  word text primary key check (char_length(word) between 2 and 40)
);

alter table public.profiles      enable row level security;
alter table public.comments      enable row level security;
alter table public.reports       enable row level security;
alter table public.blocked_words enable row level security;
revoke all on table public.profiles, public.comments, public.reports, public.blocked_words from anon, authenticated;

-- ------------------------------------------------------------------ Filtre anti-insultes

-- Met le texte sous une forme comparable : minuscules, sans accents, chiffres « leet » remplacés
-- (c0nn4rd → connard), ponctuation retirée, lettres répétées réduites (connnnard → conard).
create or replace function public.lq_normalize(t text)
returns text language sql stable set search_path = '' as $$
  select btrim(regexp_replace(regexp_replace(
           translate(lower(extensions.unaccent(coalesce(t, ''))), '013457@$€', 'oieastase'),
         '[^a-z]+', ' ', 'g'), '(.)\1+', '\1', 'g'))
$$;

-- Renvoie la raison du filtrage, ou null si le texte est correct.
create or replace function public.lq_filter_reason(t text)
returns text language plpgsql stable set search_path = '' as $$
declare
  norm   text := public.lq_normalize(t);
  spaced text := '';
  run    text[];
  w      record;
  nw     text;
begin
  if t ~* '(https?://|www\.|[a-z0-9-]+\.(com|net|org|ru|xyz|io|gg|fr|be|ly|me|link|click|top|shop)(/|\s|$))' then
    return 'lien (anti-spam)';
  end if;
  -- Lettres espacées pour contourner le filtre (« c o n n a r d ») : on les recolle.
  for run in select regexp_matches(norm, '((?:\m[a-z] ){2,}[a-z]\M)', 'g') loop
    spaced := spaced || ' ' || regexp_replace(replace(run[1], ' ', ''), '(.)\1+', '\1', 'g');
  end loop;
  for w in select word from public.blocked_words loop
    nw := public.lq_normalize(w.word);
    continue when nw = '';
    -- Mot entier, au singulier ou au pluriel (connard, connards, connasses…)
    if norm ~ ('\m' || nw || '(e|s|es|x)?\M') or spaced ~ ('\m' || replace(nw, ' ', '') || '(e|s|es|x)?\M') then
      return 'mot filtré : ' || w.word;
    end if;
  end loop;
  return null;
end $$;

-- ------------------------------------------------------------------ Pseudos

create or replace function public.lq_pseudo_error(p text, uid uuid)
returns text language plpgsql stable set search_path = '' as $$
begin
  if p is null or p !~ '^[A-Za-z0-9_.-]{2,20}$' then
    return 'Le pseudo doit faire 2 à 20 caractères : lettres, chiffres, point, tiret ou tiret bas.';
  end if;
  if lower(p) in ('admin', 'administrateur', 'moderateur', 'modo', 'lunaquest', 'anonyme', 'support', 'createur', 'staff') then
    return 'Ce pseudo est réservé.';
  end if;
  if public.lq_filter_reason(p) is not null then
    return 'Ce pseudo n''est pas autorisé.';
  end if;
  if exists (select 1 from public.profiles where lower(pseudo) = lower(p) and id is distinct from uid) then
    return 'Ce pseudo est déjà pris.';
  end if;
  -- Contre l'usurpation : « L.D », « l_d » ou « Ld. » sont trop proches de « LD ».
  if exists (select 1 from public.profiles
             where regexp_replace(lower(pseudo), '[._-]', '', 'g') = regexp_replace(lower(p), '[._-]', '', 'g')
               and id is distinct from uid) then
    return 'Ce pseudo est trop proche d''un pseudo existant.';
  end if;
  return null;
end $$;

-- Crée le profil à l'inscription. Un pseudo invalide ou pris est remplacé par « membre-xxxxxx »
-- (le site vérifie le pseudo avant, donc ça n'arrive qu'en cas de course entre deux inscriptions).
create or replace function public.lq_handle_new_user()
returns trigger language plpgsql security definer set search_path = '' as $$
declare
  wanted text := new.raw_user_meta_data ->> 'pseudo';
  fallback text := 'membre-' || substr(replace(new.id::text, '-', ''), 1, 6);
begin
  begin
    insert into public.profiles (id, pseudo)
    values (new.id, case when public.lq_pseudo_error(wanted, new.id) is null then wanted else fallback end);
  exception when unique_violation then
    insert into public.profiles (id, pseudo) values (new.id, fallback);
  end;
  return new;
end $$;

drop trigger if exists lq_on_auth_user_created on auth.users;
create trigger lq_on_auth_user_created
  after insert on auth.users for each row execute function public.lq_handle_new_user();


create or replace function public.lq_is_admin()
returns boolean language sql stable security definer set search_path = '' as $$
  select exists (select 1 from public.profiles where id = auth.uid() and is_admin and not banned)
$$;

-- ------------------------------------------------------------------ Fonctions pour tous

create or replace function public.pseudo_available(p_pseudo text)
returns text language sql stable security definer set search_path = '' as $$
  select public.lq_pseudo_error(p_pseudo, auth.uid())
$$;

create or replace function public.get_public_comments(p_limit int default 20, p_before bigint default null)
returns table (id bigint, body text, created_at timestamptz, author text, is_creator boolean, is_mine boolean)
language sql stable security definer set search_path = '' as $$
  select c.id, c.body, c.created_at,
         case when c.anonymous then 'Anonyme' else p.pseudo end,
         (not c.anonymous and p.is_admin),
         (auth.uid() is not null and c.user_id = auth.uid())
  from public.comments c
  join public.profiles p on p.id = c.user_id
  where c.status = 'public' and (p_before is null or c.id < p_before)
  order by c.id desc
  limit least(greatest(coalesce(p_limit, 20), 1), 50)
$$;

-- ------------------------------------------------------------------ Fonctions pour les membres

create or replace function public.my_profile()
returns table (pseudo text, is_admin boolean, banned boolean)
language sql stable security definer set search_path = '' as $$
  select pseudo, is_admin, banned from public.profiles where id = auth.uid()
$$;

create or replace function public.post_comment(p_body text, p_anonymous boolean default false)
returns text language plpgsql security definer set search_path = '' as $$
declare
  uid    uuid := auth.uid();
  prof   public.profiles;
  v_body text := btrim(regexp_replace(coalesce(p_body, ''), '\n{3,}', E'\n\n', 'g'));
  reason text;
begin
  if uid is null then raise exception 'Connecte-toi pour commenter.'; end if;
  select * into prof from public.profiles where id = uid;
  if not found then raise exception 'Profil introuvable.'; end if;
  if prof.banned then raise exception 'Ton compte ne peut plus publier de commentaires.'; end if;
  if char_length(v_body) < 2 or char_length(v_body) > 1000 then
    raise exception 'Ton commentaire doit faire entre 2 et 1000 caractères.';
  end if;
  -- Anti-spam : 2 commentaires par minute, 20 par jour, pas deux fois le même texte.
  if (select count(*) from public.comments c where c.user_id = uid and c.created_at > now() - interval '1 minute') >= 2
     or (select count(*) from public.comments c where c.user_id = uid and c.created_at > now() - interval '1 day') >= 20 then
    raise exception 'Tu publies trop vite. Réessaie dans quelques minutes.';
  end if;
  if exists (select 1 from public.comments c where c.user_id = uid and c.body = v_body and c.created_at > now() - interval '1 day') then
    raise exception 'Tu as déjà publié ce commentaire.';
  end if;
  reason := public.lq_filter_reason(v_body);
  insert into public.comments (user_id, body, anonymous, status, flag_reason)
  values (uid, v_body, coalesce(p_anonymous, false), case when reason is null then 'public' else 'hidden' end, reason);
  return case when reason is null then 'public' else 'hidden' end;
end $$;

-- L'auteur peut supprimer son commentaire pour de vrai.
create or replace function public.delete_my_comment(p_id bigint)
returns void language sql security definer set search_path = '' as $$
  delete from public.comments where id = p_id and user_id = auth.uid()
$$;

-- 3 signalements de membres différents masquent le commentaire en attendant l'admin.
create or replace function public.report_comment(p_id bigint)
returns void language plpgsql security definer set search_path = '' as $$
declare uid uuid := auth.uid();
begin
  if uid is null then raise exception 'Connecte-toi pour signaler.'; end if;
  if exists (select 1 from public.profiles where id = uid and banned) then raise exception 'Action impossible.'; end if;
  if not exists (select 1 from public.comments where id = p_id and status = 'public' and user_id <> uid) then return; end if;
  insert into public.reports (comment_id, user_id) values (p_id, uid) on conflict do nothing;
  if (select count(*) from public.reports where comment_id = p_id) >= 3 then
    update public.comments set status = 'hidden', flag_reason = 'signalé par plusieurs membres', moderated_at = now()
    where id = p_id and status = 'public';
  end if;
end $$;

create or replace function public.set_my_pseudo(p_pseudo text)
returns void language plpgsql security definer set search_path = '' as $$
declare err text;
begin
  if auth.uid() is null then raise exception 'Connecte-toi d''abord.'; end if;
  if exists (select 1 from public.profiles where id = auth.uid() and banned) then raise exception 'Action impossible.'; end if;
  err := public.lq_pseudo_error(p_pseudo, auth.uid());
  if err is not null then raise exception '%', err; end if;
  update public.profiles set pseudo = p_pseudo where id = auth.uid();
end $$;

-- Supprime le compte, le profil et tous les commentaires du membre (droit à l'effacement).
create or replace function public.delete_my_account()
returns void language sql security definer set search_path = '' as $$
  delete from auth.users where id = auth.uid()
$$;

-- ------------------------------------------------------------------ Fonctions d'administration
-- Chacune vérifie que l'appelant est admin (profiles.is_admin), sinon elle refuse.

create or replace function public.admin_list_comments(p_status text default 'hidden', p_limit int default 50, p_before bigint default null)
returns table (id bigint, body text, created_at timestamptz, status text, flag_reason text, anonymous boolean,
               author_id uuid, author_pseudo text, author_banned boolean, reports bigint)
language plpgsql stable security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  return query
    select c.id, c.body, c.created_at, c.status, c.flag_reason, c.anonymous, p.id, p.pseudo, p.banned,
           (select count(*) from public.reports r where r.comment_id = c.id)
    from public.comments c join public.profiles p on p.id = c.user_id
    where (p_status = 'all' or c.status = p_status) and (p_before is null or c.id < p_before)
    order by c.id desc
    limit least(greatest(coalesce(p_limit, 50), 1), 200);
end $$;

create or replace function public.admin_set_comment_status(p_id bigint, p_status text)
returns void language plpgsql security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  if p_status not in ('public', 'hidden', 'deleted') then raise exception 'Statut inconnu.'; end if;
  update public.comments
  set status = p_status, moderated_at = now(),
      flag_reason = case p_status when 'public' then null when 'hidden' then 'masqué par l''admin' else 'supprimé par l''admin' end
  where id = p_id;
  if p_status = 'public' then delete from public.reports where comment_id = p_id; end if;
end $$;

create or replace function public.admin_ban_user(p_user uuid, p_reason text default null, p_hide_comments boolean default true)
returns void language plpgsql security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  if p_user = auth.uid() or exists (select 1 from public.profiles where id = p_user and is_admin) then
    raise exception 'Impossible de bloquer un administrateur.';
  end if;
  update public.profiles set banned = true, banned_reason = left(p_reason, 200), banned_at = now() where id = p_user;
  if p_hide_comments then
    update public.comments set status = 'hidden', flag_reason = 'compte bloqué', moderated_at = now()
    where user_id = p_user and status = 'public';
  end if;
end $$;

create or replace function public.admin_unban_user(p_user uuid)
returns void language plpgsql security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  update public.profiles set banned = false, banned_reason = null, banned_at = null where id = p_user;
end $$;

create or replace function public.admin_list_banned()
returns table (id uuid, pseudo text, banned_reason text, banned_at timestamptz)
language plpgsql stable security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  return query select p.id, p.pseudo, p.banned_reason, p.banned_at from public.profiles p where p.banned order by p.banned_at desc;
end $$;

create or replace function public.admin_list_words()
returns setof text language plpgsql stable security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  return query select word from public.blocked_words order by word;
end $$;

create or replace function public.admin_add_word(p_word text)
returns void language plpgsql security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  insert into public.blocked_words (word) values (lower(btrim(p_word))) on conflict do nothing;
end $$;

create or replace function public.admin_remove_word(p_word text)
returns void language plpgsql security definer set search_path = '' as $$
begin
  if not public.lq_is_admin() then raise exception 'Réservé à l''administrateur.'; end if;
  delete from public.blocked_words where word = lower(btrim(p_word));
end $$;

-- ------------------------------------------------------------------ Droits d'exécution
-- Par défaut, Supabase laisse tout le monde appeler les fonctions : on ferme tout,
-- puis on ouvre seulement ce qu'il faut.

revoke execute on all functions in schema public from public, anon, authenticated;
grant execute on function public.lq_handle_new_user() to supabase_auth_admin;
grant execute on function public.pseudo_available(text)               to anon, authenticated;
grant execute on function public.get_public_comments(int, bigint)     to anon, authenticated;
grant execute on function public.my_profile()                         to authenticated;
grant execute on function public.post_comment(text, boolean)          to authenticated;
grant execute on function public.delete_my_comment(bigint)            to authenticated;
grant execute on function public.report_comment(bigint)               to authenticated;
grant execute on function public.set_my_pseudo(text)                  to authenticated;
grant execute on function public.delete_my_account()                  to authenticated;
grant execute on function public.admin_list_comments(text, int, bigint) to authenticated;
grant execute on function public.admin_set_comment_status(bigint, text) to authenticated;
grant execute on function public.admin_ban_user(uuid, text, boolean)  to authenticated;
grant execute on function public.admin_unban_user(uuid)               to authenticated;
grant execute on function public.admin_list_banned()                  to authenticated;
grant execute on function public.admin_list_words()                   to authenticated;
grant execute on function public.admin_add_word(text)                 to authenticated;
grant execute on function public.admin_remove_word(text)              to authenticated;

-- ------------------------------------------------------------------ Mots filtrés de départ
-- Modifiables ensuite depuis la page d'administration du site.

insert into public.blocked_words (word) values
  ('connard'), ('connasse'), ('salope'), ('salaud'), ('pute'), ('fils de pute'), ('fdp'), ('ntm'),
  ('nique ta mere'), ('niquer'), ('encule'), ('enculer'), ('ta gueule'), ('tg'), ('batard'), ('petasse'),
  ('grognasse'), ('pd'), ('pede'), ('tapette'), ('gouine'), ('negre'), ('negro'), ('bougnoule'), ('youpin'),
  ('chinetoque'), ('bicot'), ('sale arabe'), ('sale noir'), ('sale juif'), ('mongolien'), ('triso'),
  ('trou du cul'), ('branleur'), ('sous merde'), ('suicide toi'), ('va crever'),
  ('fuck you'), ('fuck off'), ('motherfucker'), ('bitch'), ('cunt'), ('asshole'), ('faggot'), ('nigger'),
  ('nigga'), ('whore'), ('slut'), ('kys'), ('kill yourself')
on conflict do nothing;

-- ------------------------------------------------------------------ Après ta propre inscription sur le site
-- Donne-toi les droits d'administrateur (remplace LD par ton pseudo si besoin) :
--
--   update public.profiles set is_admin = true where pseudo = 'LD';
