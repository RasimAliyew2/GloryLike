# Outlook email verification setup

The registration flow now creates a pending registration, sends a six-digit
code, and creates the user only after the code is verified. The code expires
after 60 seconds. A new code can be requested after the same 60-second
cooldown.

## 1. Apply the included migration

The migration is already included in the project. Do not create another
migration for the same change.

```powershell
Update-Database
```

Or with the .NET CLI:

```bash
dotnet ef database update
```

This adds the `PendingEmailRegistrations` table and the registration fields
to `Users`.

## 2. Configure Outlook through Microsoft Graph

Create a Microsoft Entra app registration, add the Microsoft Graph
`Mail.Send` **Application** permission, and grant admin consent. The sender
must be a mailbox in that Microsoft 365 tenant.

Create a client secret for the app. Keep it outside `appsettings.json`. For
local development, run these commands from the BackendApp directory and
replace the example values:

```bash
dotnet user-secrets set "OutlookMail:TenantId" "YOUR_TENANT_ID"
dotnet user-secrets set "OutlookMail:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "OutlookMail:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "OutlookMail:SenderEmail" "sender@yourcompany.com"
```

The backend uses the OAuth 2.0 client credentials flow and Microsoft Graph's
`/users/{sender}/sendMail` endpoint. No mailbox password is stored.

For deployed environments, the equivalent environment variables are:

```text
OutlookMail__TenantId
OutlookMail__ClientId
OutlookMail__ClientSecret
OutlookMail__SenderEmail
```

## 3. Run both applications

Start BackendApp first, then WebApp. WebApp uses `Backend:BaseUrl` from its
existing configuration.

Registration API endpoints:

- `POST /api/Auth/register/email/start`
- `GET /api/Auth/register/email/{verificationId}/status`
- `POST /api/Auth/register/email/verify`
- `POST /api/Auth/register/email/resend`

## Behaviour

- An email already present in `Users` is rejected before a code is sent.
- Email uniqueness is checked again immediately before the user is created.
- Passwords and verification codes are stored only as hashes.
- A code can be tried at most five times.
- Resend creates a new code and invalidates the previous code.
