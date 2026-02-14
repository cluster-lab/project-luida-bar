using ClusterVR.CreatorKit.Editor.Api.ExternalEndpoint;
using ClusterVR.CreatorKit.Editor.Api.Exceptions;
using ClusterVR.CreatorKit.Editor.Api.User;
using ClusterVR.CreatorKit.Editor.Builder;
using ClusterVR.CreatorKit.Editor.Repository;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Assets.KaomoLab.CSEmulator.Editor.Preview;

/// <summary>
/// A helper class to return the result of the LUIDA token search.
/// </summary>
public class LuidaVerifyTokenResult
{
    public string Token { get; }
    public bool FoundAssociatedToken { get; }

    /// <summary>
    /// True if the token came from the "custom" EditorPrefs key.
    /// </summary>
    public bool IsCustomToken { get; }

    public ExternalCallVerifyToken[] AllTokens { get; }

    /// <summary>
    /// True if the user has reached the maximum number of verify tokens.
    /// </summary>
    public bool IsAtTokenCapacity { get; }

    public LuidaVerifyTokenResult(string token, bool found, bool isCustom, ExternalCallVerifyToken[] allTokens, bool isAtCapacity)
    {
        Token = token;
        FoundAssociatedToken = found;
        IsCustomToken = isCustom;
        AllTokens = allTokens ?? Array.Empty<ExternalCallVerifyToken>();
        IsAtTokenCapacity = isAtCapacity;
    }
}


/// <summary>
/// Manages Cluster API operations for endpoints and tokens without a UI.
/// </summary>
public class ClusterExternalEndpointAndVerifyTokenAccessor
{
    private static readonly TokenAuthRepository authRepo = TokenAuthRepository.Instance;
    private static readonly ExternalCallEndpointRepository endpointRepo = ExternalCallEndpointRepository.Instance;
    private static readonly ExternalCallVerifyTokenRepository tokenRepo = ExternalCallVerifyTokenRepository.Instance;

    private UserInfo? cachedUserInfo;
    
    private const string CustomTokenKey = "luida_external_call_verify_token_custom";

    private string GetEditorPrefsKey(string tokenId) => $"luida_external_call_verify_token_{tokenId}";

    private async Task<UserInfo?> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        if (cachedUserInfo.HasValue)
        {
            return cachedUserInfo.Value;
        }

        if (authRepo.UserInfo.Val.HasValue)
        {
            cachedUserInfo = authRepo.UserInfo.Val;
            return cachedUserInfo;
        }

        var authInfo = EditorPrefsUtils.SavedAccessToken;
        if (string.IsNullOrEmpty(authInfo?.RawValue))
        {
            Debug.LogError("Cluster Access Token not found in EditorPrefs. Please log in via the Cluster Creator Kit window first.");
            return null;
        }

        try
        {
            await authRepo.LoginAsync(authInfo, cancellationToken);
            
            if (!authRepo.UserInfo.Val.HasValue)
            {
                Debug.LogError("Login failed with the saved token. It may be invalid or expired. Please re-login via the Cluster Creator Kit window.");
                return null;
            }

            cachedUserInfo = authRepo.UserInfo.Val;
            return cachedUserInfo;
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred during login: {ex.Message}");
            return null;
        }
    }

    private async Task<ExternalCallVerifyToken> CreateNewTokenAndSaveToPrefsAsync(UserInfo userInfo, CancellationToken cancellationToken)
    {
        Debug.Log("Creating a new verify token...");
        await tokenRepo.RegisterVerifyTokenAsync(userInfo.VerifiedToken, cancellationToken);

        var newTokensList = tokenRepo.VerifyTokenList.Val;
        var newToken = newTokensList?.LastOrDefault();

        if (newToken == null || string.IsNullOrEmpty(newToken.VerifyToken))
        {
            throw new Exception("Failed to retrieve new verify token after creation.");
        }

        string key = GetEditorPrefsKey(newToken.TokenId);
        EditorPrefs.SetString(key, newToken.VerifyToken);
        
        Debug.Log($"New token created and saved to EditorPrefs with key: {key}");
        return newToken;
    }

    public async Task<ExternalCallEndpoint> GetOrCreateEndpointAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        var userInfo = await GetUserInfoAsync(cancellationToken);
        if (!userInfo.HasValue)
        {
            throw new InvalidOperationException("Authentication failed. Cannot get or create endpoint.");
        }

        try
        {
            await endpointRepo.LoadEndpointListAsync(userInfo.Value.VerifiedToken, cancellationToken);

            var endpoints = endpointRepo.EndpointList.Val;
            var existingEndpoint = endpoints?.FirstOrDefault(e => e.Url == targetUrl);

            if (existingEndpoint != null)
            {
                Debug.Log($"Found existing endpoint for URL: {targetUrl}");
                SyncEmulatorOptions(existingEndpoint);
                return existingEndpoint;
            }

            Debug.Log($"No endpoint found for URL: {targetUrl}. Creating new one...");
            await endpointRepo.RegisterEndpointAsync(userInfo.Value.VerifiedToken, targetUrl, cancellationToken);

            var newEndpoint = endpointRepo.EndpointList.Val?.FirstOrDefault(e => e.Url == targetUrl);

            if (newEndpoint == null)
            {
                throw new Exception("Failed to retrieve endpoint immediately after creation.");
            }
            
            Debug.Log("Successfully created and retrieved new endpoint.");
            SyncEmulatorOptions(newEndpoint);
            return newEndpoint;
        }
        catch (ExternalCallInvalidUrlException)
        {
            Debug.LogError($"Failed to create endpoint: The URL '{targetUrl}' is invalid.");
            throw;
        }
        catch (ExternalCallEndpointCountLimitExceededException)
        {
            Debug.LogError("Failed to create endpoint: You have reached the maximum number of endpoints.");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while getting or creating an endpoint: {ex.Message}");
            throw;
        }
    }
    
    public async Task<LuidaVerifyTokenResult> GetLuidaVerifyTokenAsync(CancellationToken cancellationToken = default)
    {
        var userInfo = await GetUserInfoAsync(cancellationToken);
        if (!userInfo.HasValue)
        {
            throw new InvalidOperationException("Authentication failed. Cannot get or create verify token.");
        }
        
        await tokenRepo.LoadVerifyTokenListAsync(userInfo.Value.VerifiedToken, cancellationToken);
        var allTokens = tokenRepo.VerifyTokenList.Val;
        bool isAtCapacity = allTokens != null && allTokens.Length >= ExternalCallVerifyToken.MaxVerifyTokenCount;

        // 1. Check for an existing, API-associated token (newest to oldest)
        if (allTokens != null && allTokens.Length > 0)
        {
            for (int i = allTokens.Length - 1; i >= 0; i--)
            {
                var token = allTokens[i];
                string key = GetEditorPrefsKey(token.TokenId);
                string savedToken = EditorPrefs.GetString(key);

                if (!string.IsNullOrEmpty(savedToken))
                {
                    Debug.Log($"Found associated verify token in EditorPrefs: {key}");
                    return new LuidaVerifyTokenResult(savedToken, true, false, allTokens, isAtCapacity);
                }
            }
        }

        // 2. No tokens exist at all. Create one.
        if (allTokens == null || allTokens.Length == 0)
        {
            var newToken = await CreateNewTokenAndSaveToPrefsAsync(userInfo.Value, cancellationToken);
            var newTokensList = tokenRepo.VerifyTokenList.Val;
            return new LuidaVerifyTokenResult(newToken.VerifyToken, true, false, newTokensList, false);
        }

        // 3. Tokens exist, but none are associated. Check for a "custom" token.
        string customToken = EditorPrefs.GetString(CustomTokenKey);
        if (!string.IsNullOrEmpty(customToken))
        {
            Debug.Log("Found custom verify token in EditorPrefs.");
            return new LuidaVerifyTokenResult(customToken, true, true, allTokens, isAtCapacity);
        }

        // 4. Tokens exist, but none are associated and no custom token is set.
        Debug.Log("Tokens found, but none are associated with LUIDA in EditorPrefs.");
        return new LuidaVerifyTokenResult(null, false, false, allTokens, isAtCapacity);
    }
    
    public async Task<string> GenerateNewLuidaVerifyTokenAsync(ExternalCallVerifyToken[] currentTokens, CancellationToken cancellationToken = default)
    {
        var userInfo = await GetUserInfoAsync(cancellationToken);
        if (!userInfo.HasValue)
        {
            throw new InvalidOperationException("Authentication failed. Cannot generate verify token.");
        }
        
        if (currentTokens != null && currentTokens.Length >= ExternalCallVerifyToken.MaxVerifyTokenCount)
        {
            throw new InvalidOperationException(
                "Cannot generate a new verify token: you have reached the maximum number of verify tokens. " +
                "Please paste an existing token or delete one via the Cluster Creator Kit window.");
        }
        
        // Clear the custom token key since we are generating a new, associated one
        EditorPrefs.DeleteKey(CustomTokenKey);
        
        var newToken = await CreateNewTokenAndSaveToPrefsAsync(userInfo.Value, cancellationToken);
        return newToken.VerifyToken;
    }
    
    /// <summary>
    /// Syncs the found or created endpoint with the CSEmulator options.
    /// </summary>
    private void SyncEmulatorOptions(ExternalCallEndpoint endpointToSync)
    {
        try
        {
            var op = Bootstrap.options;
            if (op == null)
            {
                Debug.LogWarning("[ClusterExternalEndpointAndVerifyTokenAccessor] CSEmulator Bootstrap.options not found. Cannot sync endpoint.");
                return;
            }

            string idToSync = endpointToSync.EndpointId;
            string urlToSync = endpointToSync.Url;

            // Check if an entry with this URL already exists
            var currentEndpoints = op.callExternalUrl;
            var existingEntry = currentEndpoints.FirstOrDefault(e => e.url == urlToSync);

            if (existingEntry != null)
            {
                // URL exists, check if the ID needs updating
                if (existingEntry.id != idToSync)
                {
                    existingEntry.id = idToSync;
                    op.callExternalUrl = currentEndpoints;  // Reassign to trigger setter and persist
                    Debug.Log($"[CSEmulator] Updated callExternalUrl ID for URL: {urlToSync}");
                }
            }
            else
            {
                // URL does not exist, add a new entry
                var newEntry = new EmulatorOptions.ExternalEndpoint
                {
                    id = idToSync,
                    url = urlToSync
                };
                op.callExternalUrl = op.callExternalUrl.Append(newEntry).ToArray();
                Debug.Log($"[CSEmulator] Added new callExternalUrl entry for URL: {urlToSync}");
            }
        }
        catch (Exception ex)
        {
            // This is a non-critical operation, so just log the error and don't block the main task.
            Debug.LogWarning($"[ClusterExternalEndpointAndVerifyTokenAccessor] Failed to sync endpoint to CSEmulator options: {ex.Message}");
        }
    }
}
