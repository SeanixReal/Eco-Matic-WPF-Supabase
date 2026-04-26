create table if not exists public.user_machine_assignments (
    user_id integer not null references public.users(user_id) on delete cascade,
    machine_id integer not null references public.vending_machines(machine_id) on delete cascade,
    created_at timestamp with time zone not null default now(),
    primary key (user_id, machine_id)
);

alter table public.user_machine_assignments enable row level security;

insert into public.user_machine_assignments (user_id, machine_id)
select user_id, assigned_machine_id
from public.users
where assigned_machine_id is not null
on conflict (user_id, machine_id) do nothing;

create index if not exists idx_user_machine_assignments_machine_id
    on public.user_machine_assignments(machine_id);

do $$
begin
    if not exists (
        select 1 from pg_policies
        where schemaname = 'public'
          and tablename = 'user_machine_assignments'
          and policyname = 'Allow client read user machine assignments'
    ) then
        create policy "Allow client read user machine assignments"
            on public.user_machine_assignments
            for select
            using (true);
    end if;

    if not exists (
        select 1 from pg_policies
        where schemaname = 'public'
          and tablename = 'user_machine_assignments'
          and policyname = 'Allow client insert user machine assignments'
    ) then
        create policy "Allow client insert user machine assignments"
            on public.user_machine_assignments
            for insert
            with check (true);
    end if;

    if not exists (
        select 1 from pg_policies
        where schemaname = 'public'
          and tablename = 'user_machine_assignments'
          and policyname = 'Allow client delete user machine assignments'
    ) then
        create policy "Allow client delete user machine assignments"
            on public.user_machine_assignments
            for delete
            using (true);
    end if;
end $$;
