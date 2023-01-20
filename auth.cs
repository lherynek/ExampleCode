 public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var xPacketaTenantId = context.HttpContext.Request.Headers[TenantHeaderKey].FirstOrDefault();
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

        if (string.IsNullOrEmpty(xPacketaTenantId))
        {
            context.Result = Unauthorized(ErrorHandler.ResultFromError(Errors.InvalidSecret));
            return;
        }

        Tenant = new TenantWrapper {TenantId = xPacketaTenantId, Success = true, Errors = null};

        var basicValidation = 