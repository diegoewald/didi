-- FinançasPro V1.6.2 — schema Supabase local-first com RLS obrigatório.
-- Execute este SQL no Supabase SQL Editor. Use somente a anon key no frontend; nunca use chave privilegiada no app.

create extension if not exists pgcrypto;

create or replace function public.set_financaspro_sync_metadata()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if new.user_id is null then
    new.user_id := auth.uid();
  end if;
  new.updated_at := coalesce(new.updated_at, now());
  return new;
end;
$$;

create or replace function public.create_financaspro_sync_table(table_name text)
returns void
language plpgsql
as $$
begin
  execute format('create table if not exists public.%I (
    id text not null,
    user_id uuid not null references auth.users(id) on delete cascade,
    data jsonb not null,
    source text default ''web'',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz,
    primary key (user_id, id)
  )', table_name);
  execute format('alter table public.%I drop constraint if exists %I', table_name, table_name || '_pkey');
  execute format('alter table public.%I add constraint %I primary key (user_id, id)', table_name, table_name || '_pkey');
  execute format('create index if not exists %I on public.%I (user_id, updated_at desc)', table_name || '_user_updated_idx', table_name);
  execute format('create index if not exists %I on public.%I (user_id, deleted_at)', table_name || '_user_deleted_idx', table_name);
  execute format('alter table public.%I enable row level security', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_select_own', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_insert_own', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_update_own', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_delete_own', table_name);
  execute format('create policy %I on public.%I for select to authenticated using (user_id = auth.uid())', table_name || '_select_own', table_name);
  execute format('create policy %I on public.%I for insert to authenticated with check (user_id = auth.uid())', table_name || '_insert_own', table_name);
  execute format('create policy %I on public.%I for update to authenticated using (user_id = auth.uid()) with check (user_id = auth.uid())', table_name || '_update_own', table_name);
  execute format('create policy %I on public.%I for delete to authenticated using (user_id = auth.uid())', table_name || '_delete_own', table_name);
  execute format('drop trigger if exists %I on public.%I', table_name || '_set_sync_metadata', table_name);
  execute format('create trigger %I before insert or update on public.%I for each row execute function public.set_financaspro_sync_metadata()', table_name || '_set_sync_metadata', table_name);
end;
$$;

select public.create_financaspro_sync_table('transactions');
select public.create_financaspro_sync_table('categories');
select public.create_financaspro_sync_table('accounts');
select public.create_financaspro_sync_table('credit_cards');
select public.create_financaspro_sync_table('budgets');
select public.create_financaspro_sync_table('goals');
select public.create_financaspro_sync_table('settings');
select public.create_financaspro_sync_table('sync_queue');
