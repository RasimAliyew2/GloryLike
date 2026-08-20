# Corrected workbook taxonomy import

Run `20260818_SyncWorkbookTaxonomy.sql` against the same SQL Server database
used by `GloryLikeBackend`.

Recommended order:

1. Back up the database.
2. Deploy the Backend and WebApp code from the package.
3. Apply backend EF Core migrations, including
   `20260818193000_AddSkillTaxonomyMetadata`.
4. Open `20260818_SyncWorkbookTaxonomy.sql` in SSMS or Azure Data Studio and
   execute the complete file from its first line. Do not execute only the data
   section: the script uses a `GO` batch separator so SQL Server can compile
   references to newly added metadata columns such as `IsActive`.
5. Restart Backend and WebApp.

The script imports the complete workbook structure:

- 18 Job Families
- 260 unique Job Family + Position records
- 113 Core Skill definitions
- 447 workbook Position Skill definitions
- 229 All Positions roles, each with five generated role-specific skills
- 3,208 final active Position + Skill assignments

Important behavior:

- `Core Skills` is never inserted as a skill. It remains an Excel section
  heading only.
- The actual rows below `Core Skills` are applied to every position in that
  Job Family.
- Position-section skills apply only to that position.
- `All Positions` uses minimum seniority: a position is available for its row
  seniority and every higher canonical seniority.
- Duplicate Job Family + Position combinations are upserted once.
- Old rows are not hard-deleted because existing vacancies can reference their
  IDs. Stale skills are marked inactive, and stale position/seniority links are
  removed so they disappear from new vacancy selectors.
- The script is transactional and idempotent. Any failed verification rolls
  the complete import back.
- The standalone script also adds missing skill metadata columns defensively.
  If a previous partial run already added one or more columns, it safely skips
  them and continues with the import.

Representative expected results:

- `Sales + Middle + Sales Manager`: 7 Core Skills + 13 position skills.
- `Sales + Middle`: `Key Account Manager` is hidden.
- `Sales + Senior + Key Account Manager`: 7 Core Skills + 10 position skills.
- `Sales + Senior`: `Sales Team Lead` is hidden.
- `Sales + Lead + Sales Team Lead`: 7 Core Skills + 5 generated position
  skills.
- `Sales + Junior + Sales Representative`: 7 Core Skills + 5 generated
  position skills.
