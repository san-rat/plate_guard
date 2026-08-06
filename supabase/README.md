# Supabase schema

Open the Supabase project SQL editor, paste the contents of `schema.sql`, and run it once for the target project. The file is DDL only; PlateGuard does not execute it.

Create the service account by hand in the Supabase dashboard and provide its connection details through the deployment environment. Use placeholders such as `<SUPABASE_URL>` and `<SUPABASE_SERVICE_KEY>` in documentation or configuration examples; never commit real URLs, keys, or passwords.

The schema enables row-level security for all synced tables. Authenticated clients receive `select`, `insert`, and `update` access only to rows where `owner_id = auth.uid()`; no delete policy is granted because sync deletions are soft deletes.
