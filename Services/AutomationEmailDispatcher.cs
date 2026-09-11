using System.Net.Mail;
using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore;
namespace GloryLikeBackend.Services;

public sealed class AutomationEmailDispatcher(AppDbContext db,SmtpRegistrationEmailSender sender,
    ILogger<AutomationEmailDispatcher> logger)
{
    public async Task ProcessDueAsync(CancellationToken ct,int? vacancyId=null)
    {
        var now=DateTime.UtcNow;
        // A crashed process may have sent the email. Do not silently send it again.
        await db.AutomationEmailDeliveries.Where(d => d.Status=="Sending" && d.LeaseUntilUtc<now)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status,"Uncertain")
                .SetProperty(d => d.LastError,"Delivery was interrupted. Check the mailbox before retrying to avoid a duplicate."),ct);
        var ids=await db.AutomationEmailDeliveries.AsNoTracking().Where(d => d.Status=="Pending" && d.NextAttemptAtUtc<=now && (!vacancyId.HasValue || d.VacancyId==vacancyId))
            .OrderBy(d => d.CreatedAtUtc).Select(d => d.Id).Take(vacancyId.HasValue?1:10).ToListAsync(ct);
        foreach(var id in ids)
        {
            var lease=DateTime.UtcNow.AddMinutes(2);
            var claimed=await db.AutomationEmailDeliveries.Where(d => d.Id==id && d.Status=="Pending" && d.NextAttemptAtUtc<=now)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status,"Sending").SetProperty(d => d.LeaseUntilUtc,lease)
                    .SetProperty(d => d.Attempts,d => d.Attempts+1),ct);
            if(claimed!=1) continue;
            var row=await db.AutomationEmailDeliveries.AsNoTracking().SingleAsync(d => d.Id==id,ct);
            var acceptedBySmtp = false;
            try
            {
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(25));
                await sender.SendAutomationAsync(row.RecipientEmail,row.Subject,row.Body,row.Id,timeout.Token);
                acceptedBySmtp = true;
                await db.AutomationEmailDeliveries.Where(d => d.Id==id && d.Status=="Sending").ExecuteUpdateAsync(s =>
                    s.SetProperty(d => d.Status,"Sent").SetProperty(d => d.SentAtUtc,DateTime.UtcNow)
                    .SetProperty(d => d.LastError,""),CancellationToken.None);
            }
            catch(Exception ex)
            {
                logger.LogError(ex,"Automation delivery {DeliveryId} failed.",id);
                var transient=ex is SmtpException smtp && (int)smtp.StatusCode>=400 && (int)smtp.StatusCode<500;
                var uncertain=acceptedBySmtp || ex is OperationCanceledException || ex is SmtpException { StatusCode:SmtpStatusCode.GeneralFailure };
                var status=uncertain ? "Uncertain":transient && row.Attempts<5 ? "Pending":"Failed";
                var message=uncertain ? "SMTP delivery could not be confirmed. Check the mailbox before retrying."
                    :ex is InvalidOperationException ? "SMTP is not configured. Check the Backend Smtp settings."
                    :$"Email was not accepted by SMTP (attempt {row.Attempts}).";
                await db.AutomationEmailDeliveries.Where(d => d.Id==id && d.Status=="Sending").ExecuteUpdateAsync(s =>
                    s.SetProperty(d => d.Status,status).SetProperty(d => d.LastError,message)
                    .SetProperty(d => d.NextAttemptAtUtc,DateTime.UtcNow.AddMinutes(Math.Pow(2,row.Attempts-1))),CancellationToken.None);
            }
        }
    }
}
public sealed class AutomationEmailWorker(IServiceScopeFactory scopes,ILogger<AutomationEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(10));
        while(!stoppingToken.IsCancellationRequested)
        {
            try { using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<AutomationEmailDispatcher>().ProcessDueAsync(stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogError(ex,"Automation email queue processing failed."); }
            try { if(!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch(OperationCanceledException) { break; }
        }
    }
}
