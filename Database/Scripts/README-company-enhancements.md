# Company enhancements database update

The code adds persisted company locations, cropped company logos, and a
location reference/snapshot on every newly created vacancy.

Apply the database change in one of these ways:

1. Preferred: run `dotnet ef database update` in `GloryLikeBackend`.
2. SSMS/Azure Data Studio: execute
   `20260819_AddCompanyLocationsAndLogo.sql` once against the same database
   used by the Backend.

Do not run both methods for the same database. The standalone script is
transactional and idempotent and records the EF migration ID when
`__EFMigrationsHistory` exists.

After the database update, deploy/restart Backend first and WebApp second.
Existing single address/city/country values are copied into the first company
location. Existing vacancies remain valid; their location snapshot is empty
until they are edited and a location is selected.
