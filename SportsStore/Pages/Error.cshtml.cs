using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SportsStore.Pages
{
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                _logger.LogError(exceptionHandlerPathFeature.Error,
                    "Exception handled at error page. Path: {Path}",
                    exceptionHandlerPathFeature.Path);
            }
        }
    }
}
