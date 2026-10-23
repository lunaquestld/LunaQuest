-- 1. Contre l'usurpation de pseudo (« L.D », « Ld. »…)
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

-- 2. Droits d'administrateur pour le compte LD
update public.profiles set is_admin = true where pseudo = 'LD';
