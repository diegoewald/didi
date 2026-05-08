-- FinançasPro V1.6 — schema Supabase local-first com RLS obrigatório.
-- Execute este SQL no Supabase SQL Editor. Não use service role key no frontend.

create extension if not exists pgcrypto;

create or replace function public.set_financaspro_user_id()
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
    id text primary key,
    user_id uuid not null references auth.users(id) on delete cascade,
    data jsonb not null,
    source text default ''web'',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz
  )', table_name);
  execute format('create index if not exists %I on public.%I (user_id, updated_at desc)', table_name || '_user_updated_idx', table_name);
  execute format('alter table public.%I enable row level security', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_select_own', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_insert_own', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_update_own', table_name);
  execute format('drop policy if exists %I on public.%I', table_name || '_delete_own', table_name);
  execute format('create policy %I on public.%I for select using (user_id = auth.uid())', table_name || '_select_own', table_name);
  execute format('create policy %I on public.%I for insert with check (user_id = auth.uid())', table_name || '_insert_own', table_name);
  execute format('create policy %I on public.%I for update using (user_id = auth.uid()) with check (user_id = auth.uid())', table_name || '_update_own', table_name);
  execute format('create policy %I on public.%I for delete using (user_id = auth.uid())', table_name || '_delete_own', table_name);
  execute format('drop trigger if exists %I on public.%I', table_name || '_set_user_id', table_name);
  execute format('create trigger %I before insert or update on public.%I for each row execute function public.set_financaspro_user_id()', table_name || '_set_user_id', table_name);
end;
$$;

select public.create_financaspro_sync_table('transactions');
select public.create_financaspro_sync_table('categories');
select public.create_financaspro_sync_table('accounts');
select public.create_financaspro_sync_table('credit_cards');
select public.create_financaspro_sync_table('budgets');
select public.create_financaspro_sync_table('goals');
select public.create_financaspro_sync_table('settings');
