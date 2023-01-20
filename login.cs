e readonly ITokenProvider _tokenProvider;
    private readonly IAuthorizationProvider _authorizationProvider;

    public LoginUserQueryHandler(SignInManager<CarmaticIdentityUser> signInManager, 
        UserManager<CarmaticIdentityUser> userManager, ITokenProvider tokenProvider,
        IAuthorizationProvider authorizationProvider)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenProvider = tokenProvider;
        _authorizationProvider = authorizationProvider;
    }

    public async Task<AuthResponseDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.User.UserName);
        if (user == null)
        {
            throw new AuthenticationException();
        }
            
        var result = await _signInManager.PasswordSignInAsync
            (request.User.UserName, request.User.Password, request.User.RememberMe, false);
        if (result.Succeeded)
        {
            var claim = await _userManager.GetClaimsAsync(user);
            var dealers = claim.Where(x => x.Type == nameof(RegisterDto.DealerId))
                .Select(x => x.Value).ToList();

            var refreshToken = _tokenProvider.GenerateRefreshToken();