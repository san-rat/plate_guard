create table public.vehicles (
    sync_id uuid primary key,
    owner_id uuid not null default auth.uid(),
    vehicle_number_raw text not null,
    vehicle_number_normalized text not null,
    phone_number text not null,
    owner_name text,
    brand text,
    model text,
    is_deleted boolean not null default false,
    deleted_at_utc timestamptz,
    created_at timestamptz not null,
    updated_at timestamptz
);

create table public.promotions (
    sync_id uuid primary key,
    owner_id uuid not null default auth.uid(),
    promotion_name text not null,
    description text,
    start_date timestamptz,
    end_date timestamptz,
    is_active boolean not null default true,
    is_deleted boolean not null default false,
    deleted_at_utc timestamptz,
    created_at timestamptz not null,
    updated_at timestamptz
);

create table public.promotion_usages (
    sync_id uuid primary key,
    owner_id uuid not null default auth.uid(),
    vehicle_sync_id uuid not null references public.vehicles (sync_id),
    promotion_sync_id uuid not null references public.promotions (sync_id),
    service_date timestamptz not null,
    mileage integer,
    normal_price numeric,
    discounted_price numeric,
    amount_paid numeric,
    notes text,
    is_deleted boolean not null default false,
    deleted_at_utc timestamptz,
    created_at timestamptz not null,
    updated_at timestamptz
);

-- Scoped by owner_id to match the RLS model: a second account must be able to hold
-- the same plate without colliding with this one.
create unique index ux_vehicles_vehicle_number_normalized_live
    on public.vehicles (owner_id, vehicle_number_normalized)
    where is_deleted = false;

create unique index ux_promotion_usages_vehicle_promotion_live
    on public.promotion_usages (vehicle_sync_id, promotion_sync_id)
    where is_deleted = false;

create index ix_vehicles_updated_at on public.vehicles (updated_at);
create index ix_promotions_updated_at on public.promotions (updated_at);
create index ix_promotion_usages_updated_at on public.promotion_usages (updated_at);

create function public.set_updated_at() returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

create trigger vehicles_set_updated_at
before insert or update on public.vehicles
for each row execute function public.set_updated_at();

create trigger promotions_set_updated_at
before insert or update on public.promotions
for each row execute function public.set_updated_at();

create trigger promotion_usages_set_updated_at
before insert or update on public.promotion_usages
for each row execute function public.set_updated_at();

alter table public.vehicles enable row level security;
alter table public.promotions enable row level security;
alter table public.promotion_usages enable row level security;

create policy vehicles_select on public.vehicles for select to authenticated using (owner_id = auth.uid());
create policy vehicles_insert on public.vehicles for insert to authenticated with check (owner_id = auth.uid());
create policy vehicles_update on public.vehicles for update to authenticated using (owner_id = auth.uid()) with check (owner_id = auth.uid());

create policy promotions_select on public.promotions for select to authenticated using (owner_id = auth.uid());
create policy promotions_insert on public.promotions for insert to authenticated with check (owner_id = auth.uid());
create policy promotions_update on public.promotions for update to authenticated using (owner_id = auth.uid()) with check (owner_id = auth.uid());

create policy promotion_usages_select on public.promotion_usages for select to authenticated using (owner_id = auth.uid());
create policy promotion_usages_insert on public.promotion_usages for insert to authenticated with check (owner_id = auth.uid());
create policy promotion_usages_update on public.promotion_usages for update to authenticated using (owner_id = auth.uid()) with check (owner_id = auth.uid());
