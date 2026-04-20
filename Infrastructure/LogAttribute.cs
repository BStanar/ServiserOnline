using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiserOnline.Models;

namespace ServiserOnline.Infrastructure;

public class LogAttribute : ActionFilterAttribute
{
    private DateTime _startTime;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _startTime = DateTime.Now;
    }

    public override async void OnResultExecuted(ResultExecutedContext context)
    {
        try
        {
            var routeData = context.RouteData;
            var duration = DateTime.Now - _startTime;
            string controller = (string)routeData.Values["controller"];
            string action = (string)routeData.Values["action"];

            if (controller == "Account")
                return;

            var sb = new StringBuilder();
            if (context.HttpContext.Request.HasFormContentType)
            {
                var form = await context.HttpContext.Request.ReadFormAsync();
                foreach (var key in form.Keys)
                {
                    if (key != "__RequestVerificationToken")
                        sb.Append($"{key}={form[key]}, ");
                }
                if (sb.Length >= 2)
                    sb.Remove(sb.Length - 2, 2);
            }

            string userName = context.HttpContext.User?.Identity?.Name;

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var audit = new AuditTrace
            {
                ID = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Duration = duration,
                Controller = controller,
                Action = action,
                Data = sb.ToString(),
                User = userName
            };
            db.AuditTrace.Add(audit);
            await db.SaveChangesAsync();

            // Clean up old entries
            var cutoff = DateTime.Now.AddMonths(-1);
            var old = db.AuditTrace.Where(x => x.CreatedDate < cutoff);
            db.AuditTrace.RemoveRange(old);
            await db.SaveChangesAsync();
        }
        catch { }
    }
}
