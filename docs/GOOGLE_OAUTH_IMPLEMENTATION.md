# Google OAuth Sign-In Implementation Guide

## 📋 Overview

This document provides a comprehensive guide on how Google OAuth Sign-In is implemented in both the frontend (Angular) and backend (.NET) of the E-Commerce application.

## 🏗️ Architecture

The Google OAuth implementation follows the **Authorization Code Flow**:

1. User clicks "Continue with Google" button
2. Frontend redirects to backend OAuth endpoint
3. Backend redirects to Google OAuth consent screen
4. User authenticates with Google
5. Google redirects back to backend callback
6. Backend exchanges code for tokens and creates/updates user
7. Backend generates JWT token and redirects to frontend
8. Frontend processes JWT token and logs user in

---

## 🔧 Backend Implementation

### 1. Google OAuth Service (`GoogleOAuthService.cs`)

**Location:** `Ecommerce.Infrastructure/Services/GoogleOAuthService.cs`

**Purpose:** Handles communication with Google OAuth APIs

**Key Methods:**

- `ExchangeCodeForTokenAsync(string code, string redirectUri)`
  - Exchanges authorization code for access token
  - Endpoint: `https://oauth2.googleapis.com/token`
  - Returns: `OAuthTokenResult` with access token, refresh token, etc.

- `GetUserInfoAsync(string accessToken)`
  - Retrieves user information from Google
  - Endpoint: `https://www.googleapis.com/oauth2/v2/userinfo`
  - Returns: `OAuthUserInfo` with email, name, picture, etc.

**Features:**
- Input validation and sanitization
- Resilience policies (retry and circuit breaker)
- Comprehensive error handling
- Support for both "sub" and "id" fields from Google

### 2. Google OAuth Controller (`GoogleOAuthController.cs`)

**Location:** `Ecommerce.API/Controllers/GoogleOAuthController.cs`

**Endpoints:**

#### `GET /api/google-oauth/login`
- Initiates Google OAuth flow
- Generates state parameter for CSRF protection
- Stores redirect URI and return URL in session
- Redirects user to Google OAuth consent screen

**Query Parameters:**
- `returnUrl` (optional): Frontend URL to redirect after authentication

**Example:**
```
GET /api/google-oauth/login?returnUrl=https://localhost:4200/login
```

#### `GET /api/google-oauth/callback`
- Handles Google OAuth callback
- Validates state parameter
- Exchanges authorization code for tokens
- Retrieves user information from Google
- **Auto-creates user in database if not exists**
- Updates user info if already exists
- Generates JWT token
- Redirects to frontend with JWT token

**Query Parameters:**
- `code`: Authorization code from Google
- `state`: State parameter for validation
- `error`: Error code if authentication failed
- `returnUrl`: Frontend URL to redirect to

**Auto-Registration Logic:**
```csharp
// Check if user exists in database
var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);

if (dbUser == null)
{
    // Create new user
    dbUser = new ApplicationUser
    {
        Email = userInfo.Email,
        FirstName = userInfo.GivenName ?? string.Empty,
        LastName = userInfo.FamilyName ?? string.Empty,
        Username = userInfo.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
        Role = "User",
        Status = "Active"
    };
    _context.Users.Add(dbUser);
    await _context.SaveChangesAsync();
}
else
{
    // Update user info if changed
    if (dbUser.FirstName != userInfo.GivenName || dbUser.LastName != userInfo.FamilyName)
    {
        dbUser.FirstName = userInfo.GivenName ?? dbUser.FirstName;
        dbUser.LastName = userInfo.FamilyName ?? dbUser.LastName;
        dbUser.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
```

#### `GET /api/google-oauth/userinfo`
- Returns authenticated user information
- Requires authentication

#### `POST /api/google-oauth/logout`
- Logs out user from Google OAuth
- Clears session cookies

### 3. Configuration

**Location:** `appsettings.json`

```json
{
  "GoogleOAuth": {
    "ClientId": "534395859742-5grrp7f1vl5g82c3i7egutrk499shbrd.apps.googleusercontent.com",
    "ClientSecret": "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",
    "Scope": "openid profile email",
    "CallbackPath": "/api/google-oauth/callback",
    "ResponseType": "code",
    "BaseUrl": "https://localhost:7273"
  },
  "Frontend": {
    "BaseUrl": "https://localhost:4200"
  }
}
```

**Important Configuration Points:**
- `ClientId`: Google OAuth Client ID
- `ClientSecret`: Google OAuth Client Secret (should be in User Secrets/Key Vault for production)
- `CallbackPath`: Backend callback path (must match Google Cloud Console)
- `BaseUrl`: Backend base URL for constructing redirect URIs
- `Frontend:BaseUrl`: Frontend URL for redirecting after authentication

### 4. Dependency Injection

**Location:** `Ecommerce.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

```csharp
// Google OAuth Service with resilience policies
services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>((provider, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler((provider, request) =>
{
    var resilienceService = provider.GetRequiredService<IResiliencePolicyService>();
    var retryPolicy = resilienceService.GetHttpRetryPolicy();
    var circuitBreakerPolicy = resilienceService.GetHttpCircuitBreakerPolicy();
    return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
});
```

---

## 🎨 Frontend Implementation

### 1. OAuth Service (`oauth.service.ts`)

**Location:** `ecommerce-ui/src/app/services/oauth.service.ts`

**Key Methods:**

#### `initiateGoogleLogin(returnUrl?: string)`
- Initiates Google OAuth login flow
- Constructs full frontend URL for return
- Redirects to backend Google OAuth endpoint

```typescript
initiateGoogleLogin(returnUrl?: string): void {
  const callbackUrl = returnUrl || `${window.location.origin}/oauth/callback`;
  const fullReturnUrl = callbackUrl.startsWith('http') 
    ? callbackUrl 
    : `${window.location.origin}${callbackUrl}`;
  const params = `?returnUrl=${encodeURIComponent(fullReturnUrl)}`;
  window.location.href = `${this.baseUrl}/google-oauth/login${params}`;
}
```

#### `getGoogleUserInfo()`
- Retrieves user information from backend
- Endpoint: `GET /api/google-oauth/userinfo`

### 2. Login Component (`login.component.ts`)

**Location:** `ecommerce-ui/src/app/login/login.component.ts`

**Key Features:**

#### JWT Token Processing
- Automatically detects JWT token in URL query parameters
- Decodes JWT token to extract user information
- Stores token in localStorage
- Sets OAuth user information
- Updates authentication state
- Redirects to home/admin page

```typescript
ngOnInit(): void {
  // Check for JWT token in query parameters (from OAuth callback)
  this.route.queryParams.subscribe(params => {
    if (params['jwt_token']) {
      console.log('JWT token detected in URL, processing OAuth login...');
      this.handleOAuthToken(params['jwt_token']);
    }
  });
}

private handleOAuthToken(token: string): void {
  this.loading = true;
  this.error = '';

  try {
    // Store the JWT token
    localStorage.setItem('accessToken', token);
    localStorage.setItem('authToken', token);

    // Decode JWT to get user info
    const payload = JSON.parse(atob(token.split('.')[1]));
    const email = payload['email'] || '';
    const firstName = payload['FirstName'] || '';
    const lastName = payload['LastName'] || '';
    const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'User';
    const sub = payload['sub'] || email;

    // Set OAuth user info
    if (email) {
      this.authService.setOAuthUser({
        sub: sub,
        email: email,
        name: `${firstName} ${lastName}`.trim() || email,
        given_name: firstName,
        family_name: lastName
      });
    }

    // Redirect based on role
    setTimeout(() => {
      if (role === 'Admin') {
        this.router.navigate(['/admin'], { replaceUrl: true });
      } else {
        this.router.navigate(['/home'], { replaceUrl: true });
      }
    }, 500);
  } catch (error: any) {
    console.error('Error handling OAuth token:', error);
    this.error = `Failed to process authentication: ${error.message}`;
    this.loading = false;
  }
}
```

#### Google OAuth Button Handler
```typescript
onGoogleOAuthLogin(): void {
  this.loading = true;
  this.error = '';
  
  // Get full frontend URL to redirect back after OAuth
  const returnUrl = `${window.location.origin}${this.router.url}`;
  
  // Initiate Google OAuth login
  this.oauthService.initiateGoogleLogin(returnUrl);
}
```

### 3. Register Component (`register.component.ts`)

**Location:** `ecommerce-ui/src/app/register/register.component.ts`

Similar implementation to login component for Google OAuth button.

### 4. Auth Service (`auth.service.ts`)

**Location:** `ecommerce-ui/src/app/auth.service.ts`

**OAuth-Specific Methods:**

#### `setOAuthUser(userInfo: OAuthUserInfo)`
- Stores OAuth user information in sessionStorage
- Updates authentication state

#### `getOAuthUser()`
- Retrieves OAuth user information from sessionStorage

#### `isOAuthAuthenticated()`
- Checks if user is authenticated via OAuth

#### `isLoggedIn()`
- Checks both traditional JWT and OAuth authentication

---

## 🔄 Complete Flow Diagram

```
┌─────────────┐
│   User      │
│  (Browser)  │
└──────┬──────┘
       │
       │ 1. Clicks "Continue with Google"
       ▼
┌─────────────────────────────────────────────┐
│  Frontend (Angular)                         │
│  - login.component.ts                       │
│  - Calls oauthService.initiateGoogleLogin() │
└──────┬──────────────────────────────────────┘
       │
       │ 2. Redirects to: /api/google-oauth/login?returnUrl=...
       ▼
┌──────────────────────────────────────────┐
│  Backend (.NET)                          │
│  - GoogleOAuthController.Login()         │
│  - Stores state & redirectUri in session │
│  - Redirects to Google OAuth             │
└──────┬───────────────────────────────────┘
       │
       │ 3. Redirects to Google
       ▼
┌──────────────────────────────────── ─┐
│  Google OAuth                        │
│  - User authenticates                │
│  - User grants permissions           │
└──────┬───────────────────────────── ─┘
       │
       │ 4. Redirects with code: /api/google-oauth/callback?code=...&state=...
       ▼
┌────────────────────────────────── ───┐
│  Backend (.NET)                      │
│  - GoogleOAuthController.Callback()  │
│  - Validates state                   │
│  - Exchanges code for tokens         │
│  - Gets user info from Google        │
│  - Creates/updates user in DB        │
│  - Generates JWT token               │
│  - Redirects to frontend with token  │
└──────┬─────────────────────────── ───┘
       │
       │ 5. Redirects to: /login?jwt_token=...
       ▼
┌──────────────────────────────── ─────┐
│  Frontend (Angular)                  │
│  - login.component.ts                │
│  - Detects JWT token in URL          │
│  - Decodes token                     │
│  - Stores token & user info          │
│  - Updates auth state                │
│  - Redirects to /home or /admin      │
└──────┬─────────────────────────── ───┘
       │
       │ 6. User is logged in!
       ▼
┌─────────────┐
│   Home      │
│   Page      │
└─────────────┘
```

---

## 🔐 Security Features

### Backend Security

1. **State Parameter Validation**
   - Prevents CSRF attacks
   - Stored in session, validated on callback

2. **Session Management**
   - Redirect URI stored in session
   - Ensures exact match during token exchange

3. **Input Sanitization**
   - All inputs sanitized before processing
   - Prevents injection attacks

4. **Resilience Policies**
   - Retry policies for transient failures
   - Circuit breaker for service unavailability

5. **Error Handling**
   - Comprehensive error logging
   - User-friendly error messages
   - No sensitive data exposed

### Frontend Security

1. **Token Storage**
   - JWT tokens stored in localStorage
   - OAuth user info in sessionStorage

2. **URL Validation**
   - Full URLs constructed to prevent redirect attacks
   - Query parameters properly encoded

3. **Authentication State**
   - Centralized auth state management
   - Automatic token validation

---

## ⚙️ Google Cloud Console Configuration

### Required Setup

1. **Create OAuth 2.0 Client ID**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Navigate to **APIs & Services** > **Credentials**
   - Click **Create Credentials** > **OAuth client ID**
   - Select **Web application**

2. **Configure Authorized Redirect URIs**
   - Add exactly: `https://localhost:7273/api/google-oauth/callback`
   - For production: `https://your-domain.com/api/google-oauth/callback`
   - **Important:** Must match exactly (including protocol, port, and path)

3. **Get Credentials**
   - Copy **Client ID**
   - Copy **Client Secret**
   - Add to `appsettings.json` or User Secrets

### Current Configuration

- **Client ID:** `534395859742-5grrp7f1vl5g82c3i7egutrk499shbrd.apps.googleusercontent.com`
- **Authorized Redirect URI:** `https://localhost:7273/api/google-oauth/callback`

---

## 🧪 Testing

### Testing the Flow

1. **Start Backend**
   ```bash
   cd EcommerceAPI/BulkyBook-POC/Ecommerce.API
   dotnet run
   ```

2. **Start Frontend**
   ```bash
   cd ecommerce-ui
   npm start
   ```

3. **Test Google Sign-In**
   - Navigate to `https://localhost:4200/login`
   - Click "Continue with Google"
   - Authenticate with Google account
   - Should redirect to home page automatically

### Expected Behavior

1. ✅ User clicks "Continue with Google"
2. ✅ Redirects to Google OAuth consent screen
3. ✅ User authenticates and grants permissions
4. ✅ Redirects back to application
5. ✅ User is automatically logged in
6. ✅ User can access all protected routes
7. ✅ User appears in database (check Users table)

### Troubleshooting

#### Error: `redirect_uri_mismatch`
- **Cause:** Redirect URI in Google Cloud Console doesn't match
- **Solution:** Ensure exact match in Google Cloud Console

#### Error: `User not found`
- **Cause:** User not created in database
- **Solution:** Check backend logs, ensure auto-registration logic runs

#### Error: `Invalid state parameter`
- **Cause:** Session expired or state mismatch
- **Solution:** Try again, ensure sessions are enabled

#### Token not processing in frontend
- **Cause:** Login component not detecting token
- **Solution:** Check browser console, verify token in URL

---

## 📝 Key Files

### Backend Files

- `Ecommerce.Infrastructure/Services/GoogleOAuthService.cs` - Google OAuth service
- `Ecommerce.API/Controllers/GoogleOAuthController.cs` - OAuth controller
- `Ecommerce.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` - DI configuration
- `Ecommerce.API/appsettings.json` - Configuration

### Frontend Files

- `ecommerce-ui/src/app/services/oauth.service.ts` - OAuth service
- `ecommerce-ui/src/app/login/login.component.ts` - Login component
- `ecommerce-ui/src/app/register/register.component.ts` - Register component
- `ecommerce-ui/src/app/auth.service.ts` - Auth service

---

## 🎯 Key Features

### ✅ Implemented Features

1. **Google OAuth Integration**
   - Full OAuth 2.0 Authorization Code Flow
   - Secure token exchange
   - User information retrieval

2. **Auto-Registration**
   - Users automatically created in database
   - User info updated on subsequent logins
   - No manual registration required

3. **JWT Token Generation**
   - Secure JWT tokens for API access
   - Includes user claims (email, name, role)
   - Compatible with existing auth system

4. **Seamless Frontend Integration**
   - Automatic token processing
   - No password required
   - Automatic redirect after login

5. **Error Handling**
   - Comprehensive error messages
   - Detailed logging
   - User-friendly error responses

6. **Security**
   - CSRF protection (state parameter)
   - Input sanitization
   - Secure session management

---

## 🔄 Future Enhancements

### Potential Improvements

1. **Refresh Token Support**
   - Implement refresh token rotation
   - Automatic token renewal

2. **Multiple OAuth Providers**
   - Microsoft OAuth (already partially implemented)
   - Facebook OAuth
   - GitHub OAuth

3. **Account Linking**
   - Link Google account to existing account
   - Merge accounts functionality

4. **Profile Picture Sync**
   - Store Google profile picture
   - Display in user profile

5. **Email Verification**
   - Skip email verification for OAuth users
   - Mark as verified automatically

---

## 📚 References

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Angular Authentication Guide](https://angular.io/guide/security)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)

---

## ✨ Summary

The Google OAuth implementation provides a seamless, secure authentication experience:

- **No passwords required** - Users authenticate with Google
- **Automatic registration** - Users created automatically on first login
- **Secure token exchange** - Industry-standard OAuth 2.0 flow
- **Seamless integration** - Works with existing authentication system
- **Production-ready** - Comprehensive error handling and security

Users can now sign in with their Google account with a single click, and the system handles all the complexity behind the scenes!

