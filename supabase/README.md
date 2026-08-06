# Supabase schema

Open the Supabase project SQL editor, paste the contents of `schema.sql`, and run it once for the target project. The file is DDL only; PlateGuard does not execute it.

Create the service account by hand in the Supabase dashboard (Authentication → Users) and provide its connection details through the deployment environment. Use placeholders such as `<SUPABASE_URL>`, `<SUPABASE_ANON_KEY>`, `<SUPABASE_ACCOUNT_EMAIL>`, and `<SUPABASE_ACCOUNT_PASSWORD>` in documentation or configuration examples; never commit real URLs, keys, or passwords.

**Never ship the `service_role` key in the desktop app.** It bypasses row-level security entirely. The app signs in as the fixed service account using the anon key plus that account's email and password, so every request is still subject to the RLS policies below.

The schema enables row-level security for all synced tables. Authenticated clients receive `select`, `insert`, and `update` access only to rows where `owner_id = auth.uid()`; no delete policy is granted because sync deletions are soft deletes.
