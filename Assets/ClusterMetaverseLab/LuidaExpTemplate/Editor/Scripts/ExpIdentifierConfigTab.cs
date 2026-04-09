using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Threading;

using ClusterVR.CreatorKit.Editor.Builder;
using ClusterVR.CreatorKit.Editor.Repository;
using ClusterVR.CreatorKit.Editor.Api.RPC;
using ClusterVR.CreatorKit.Editor.Api.ExternalEndpoint;

public class ExpIdentifierConfigTab : EditorWindow
{
    private string prevExpID = "";
    private string prevToken = "";
    private string prevCallExternalEndpointID = "";
    private int prevPNum = 1;
    private string expID = "";
    private string token = "";
    private string callExternalEndpointID = "";
    private int pNum = 1;
    
    private string filePath;
    private const string formPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/Questionnaire/Questionnaire.prefab";
    private const string StateQuestionnaireRootName = "LUIDA-QuestionnaireByState";

    private ClusterExternalEndpointAndVerifyTokenAccessor endpointAndTokenAccessor;
    private bool isLoggedIn = false;
    private string loginTokenInput = "";
    private bool isBusy = false;
    
    // State management UI for verify token configuration
    private enum TokenUIState { ReadOnly, EditableWithGenerate, EditableCustom }
    private TokenUIState tokenUIState = TokenUIState.ReadOnly;
    private const string CustomTokenKey = "luida_external_call_verify_token_custom";
    
    private ExternalCallVerifyToken[] currentTokensList = null;
    private bool isAtTokenCapacity = false;
    private CancellationTokenSource cancellationTokenSource;

    
    [MenuItem("LUIDA/Configure experiment identifiers")]
    public static void ShowWindow()
    {
        GetWindow<ExpIdentifierConfigTab>("LUIDA Experiment Identifiers Config Window");
    }
    
    public void OnEnable()
    {
        filePath = "Assets/_Experiment_/Settings/ExpIdentifiers.js";
        
        endpointAndTokenAccessor = new ClusterExternalEndpointAndVerifyTokenAccessor();
        cancellationTokenSource = new CancellationTokenSource();

        CheckLoginState();

        if (File.Exists(filePath))
        {
            LoadExpIdentifiers();
        }
        
        prevExpID = expID;
        prevToken = token;
        prevCallExternalEndpointID = callExternalEndpointID;
        prevPNum = pNum;

        if (isLoggedIn)
        {
            AutoFillFieldsAsync(isLoginAttempt: false);
        }
    }
    
    private void OnDisable()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    private void CheckLoginState()
    {
        var savedToken = EditorPrefsUtils.SavedAccessToken;
        isLoggedIn = !string.IsNullOrEmpty(savedToken?.RawValue);
    }

    public void OnGUI()
    {
        if (isBusy)
        {
            EditorGUILayout.LabelField("Processing, please wait...");
            return;
        }

        if (!isLoggedIn)
        {
            DrawLoginGUI();
        }
        else
        {
            DrawMainConfigGUI();
        }
    }

    private void DrawLoginGUI()
    {
        EditorGUILayout.HelpBox("Please log in with your Cluster access token to auto-fill API keys.", MessageType.Info);

        if (GUILayout.Button("Create Access Token"))
        {
            Application.OpenURL("https://cluster.mu/account/tokens");
        }

        loginTokenInput = EditorGUILayout.TextField("Access Token", loginTokenInput);

        GUI.enabled = !string.IsNullOrEmpty(loginTokenInput);
        if (GUILayout.Button("Login"))
        {
            var authInfo = new AuthenticationInfo(loginTokenInput);
            EditorPrefsUtils.SavedAccessToken = authInfo;
            
            AutoFillFieldsAsync(isLoginAttempt: true);
        }
        GUI.enabled = true;
    }

    private void DrawMainConfigGUI()
    {
        if (GUILayout.Button("Logout / Switch Account"))
        {
            Logout();
            return;
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Refresh Token & Endpoint ID"))
        {
            AutoFillFieldsAsync(isLoginAttempt: false);
        }

        EditorGUILayout.Space();
        
        string newExpID = EditorGUILayout.TextField("Experiment ID", expID);
        int newPNum = EditorGUILayout.IntField("Number of Participants", pNum);

        // --- Modified Verify Token UI ---
        EditorGUILayout.LabelField("Verify Token");
        string newDrawnToken;

        switch (tokenUIState)
        {
            case TokenUIState.EditableWithGenerate:
                newDrawnToken = EditorGUILayout.TextField(token);
                if (isAtTokenCapacity)
                {
                    EditorGUILayout.HelpBox(
                        "You have reached the maximum number of verify tokens. " +
                        "Please paste one of your existing verify tokens into the field above. " +
                        "If you don’t remember any existing verify token, select [Cluster > External Communication (callExternal) Destination URL] " +
                        "from the top menu to delete the existing verify token and generate a new one.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("No associated token found. Manually paste a token or generate a new one.", MessageType.Info);
                    if (GUILayout.Button("Generate a new verify token"))
                    {
                        GenerateNewTokenAsync();
                    }
                }
                break;
            
            case TokenUIState.EditableCustom:
                newDrawnToken = EditorGUILayout.TextField(token);
                EditorGUILayout.HelpBox("Using a manually entered custom token.", MessageType.Warning);
                break;
            
            case TokenUIState.ReadOnly:
            default:
                EditorGUILayout.SelectableLabel(token, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                newDrawnToken = token;
                break;
        }
        
        EditorGUILayout.LabelField("callExternal Endpoint ID (Auto-filled)");
        EditorGUILayout.SelectableLabel(callExternalEndpointID, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        
        bool hasChanged = false;

        if (newExpID != prevExpID)
        {
            expID = newExpID;
            prevExpID = newExpID;
            hasChanged = true;
        }

        if (newDrawnToken != prevToken)
        {
            token = newDrawnToken;
            prevToken = newDrawnToken;
            hasChanged = true;
            
            // If user manually changed the token, save it as custom
            if (tokenUIState == TokenUIState.EditableWithGenerate)
            {
                EditorPrefs.SetString(CustomTokenKey, token);
                if (!string.IsNullOrEmpty(token)) // If user typed something
                {
                    tokenUIState = TokenUIState.EditableCustom; // Switch state
                }
            }
            else if (tokenUIState == TokenUIState.EditableCustom)
            {
                EditorPrefs.SetString(CustomTokenKey, token);
            }
        }

        if (newPNum != prevPNum)
        {
            pNum = newPNum;
            prevPNum = newPNum;
            hasChanged = true;
            UpdateQuestionnaireObjects();
        }

        if (hasChanged)
        {
            SaveExpIdentifiers();
        }
    }

    private void Logout()
    {
        EditorPrefsUtils.SavedAccessToken = null;
        isLoggedIn = false;
        loginTokenInput = "";
        TokenAuthRepository.Instance.Logout();
        Repaint();
    }

    private async void AutoFillFieldsAsync(bool isLoginAttempt)
    {
        if (isBusy) return;
        isBusy = true;
        bool loginSuccess = false;
        Repaint();

        try
        {
            var tokenResult = await endpointAndTokenAccessor.GetLuidaVerifyTokenAsync(cancellationTokenSource.Token);
            
            currentTokensList = tokenResult.AllTokens;
            isAtTokenCapacity = tokenResult.IsAtTokenCapacity;

            if (tokenResult.FoundAssociatedToken)
            {
                token = tokenResult.Token;
                tokenUIState = tokenResult.IsCustomToken ? TokenUIState.EditableCustom : TokenUIState.ReadOnly;
            }
            else
            {
                token = "";
                tokenUIState = TokenUIState.EditableWithGenerate;
            }

            var endpoint = await endpointAndTokenAccessor.GetOrCreateEndpointAsync("https://luida.cluster.mu/api/cluster", cancellationTokenSource.Token);

            if (isLoginAttempt)
            {
                isLoggedIn = true;
                loginTokenInput = "";
                Debug.Log("Login successful. API fields auto-filled.");
                loginSuccess = true;
            }
            else
            {
                Debug.Log("Successfully refreshed API fields.");
            }
            
            callExternalEndpointID = endpoint.EndpointId;

            prevToken = token;
            prevCallExternalEndpointID = callExternalEndpointID;

            SaveExpIdentifiers();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch Cluster API data: {ex.Message}");
            if (isLoginAttempt)
            {
                EditorPrefsUtils.SavedAccessToken = null;
                isLoggedIn = false;
                EditorUtility.DisplayDialog("Login Failed", $"Login failed. The token may be invalid or expired.\n\nError: {ex.Message}", "OK");
            }
            else
            {
                Debug.LogWarning("Token might be expired. Please log out and log in again to refresh API fields.");
            }
        }
        finally
        {
            isBusy = false;
            Repaint();
            
            if (isLoginAttempt && loginSuccess)
            {
                this.Close();
                ShowWindow();
            }
        }
    }

    private async void GenerateNewTokenAsync()
    {
        if (isBusy) return;
        isBusy = true;
        Repaint();

        try
        {
            string newToken = await endpointAndTokenAccessor.GenerateNewLuidaVerifyTokenAsync(currentTokensList, cancellationTokenSource.Token);
            
            token = newToken;
            prevToken = newToken;
            tokenUIState = TokenUIState.ReadOnly; // New token is associated
            currentTokensList = null;
            
            SaveExpIdentifiers();
            Debug.Log("Successfully generated and saved new verify token.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to generate new token: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to generate new token: {ex.Message}", "OK");
        }
        finally
        {
            isBusy = false;
            Repaint();
        }
    }

    private void LoadExpIdentifiers()
    {
        if (!File.Exists(filePath)) return;
        string content = File.ReadAllText(filePath);

        expID = ExtractStringValue(content, "expID");
        token = ExtractStringValue(content, "token");
        callExternalEndpointID = ExtractStringValue(content, "callExternalEndpointID");
        pNum = ExtractIntValue(content, "pNum");
    }

    private string ExtractStringValue(string content, string key)
    {
        var pattern = $@"{key}\s*=\s*""([^""]+)"";";
        var match = Regex.Match(content, pattern);
        return match.Success ? match.Groups[1].Value : "";
    }

    private int ExtractIntValue(string content, string key)
    {
        var pattern = $@"{key}\s*=\s*(\d+);";
        var match = Regex.Match(content, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 1;
    }

    private void SaveExpIdentifiers()
    {
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        string content =
            $"expID = \"{expID}\";\n" +
            $"token = \"{token}\";\n" +
            $"callExternalEndpointID = \"{callExternalEndpointID}\";\n" +
            $"pNum = {pNum};\n" +
            $"isTestMode = true;\n";

        File.WriteAllText(filePath, content);

        AssetDatabase.Refresh();

        Debug.Log($"Experiment identifiers saved to {filePath}");
    }

    private void UpdateQuestionnaireObjects()
    {
        // This method remains unchanged as it relies on external classes
        // (LuidaConfigWindow, QuestionnaireEditorManager, etc.)
        
        int newPNum = pNum;
        var wnd = LuidaConfigWindow.Instance; 
        
        var questionnaireGroups = new Dictionary<Transform, List<GameObject>>();
        var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var go in allGameObjects)
        {
            if (!go.scene.isLoaded || PrefabUtility.GetNearestPrefabInstanceRoot(go) != go)
            {
                continue;
            }

            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == formPrefabPath)
            {
                var parent = go.transform.parent;
                if (parent == null) continue;

                if (!questionnaireGroups.ContainsKey(parent))
                {
                    questionnaireGroups[parent] = new List<GameObject>();
                }
                questionnaireGroups[parent].Add(go);
            }
        }

        foreach (var group in questionnaireGroups)
        {
            var parentTransform = group.Key;
            var childrenCount = group.Value.Count;

            if (childrenCount == newPNum) continue;
            
            if (parentTransform.parent != null && parentTransform.parent.name == StateQuestionnaireRootName)
            {
                if (wnd != null && wnd.StateTab != null && wnd.StateTab.stateList != null)
                {
                    string stateName = parentTransform.name;
                    var stateList = wnd.StateTab.stateList;

                    for (int i = 0; i < stateList.States.Length; i++)
                    {
                        if (stateList.States[i].StateName == stateName && stateList.States[i].qID > 0)
                        {
                            Debug.Log($"Updating state-linked questionnaire '{stateName}' to have {newPNum} forms.");
                            QuestionnaireEditorManager.AddOrEnableQuestionnaireForm(stateList, i, stateName, newPNum);
                            break;
                        }
                    }
                }
            }
            else
            {
                LuidaQuestionnaire idSync = parentTransform.GetComponent<LuidaQuestionnaire>();
                if (idSync != null)
                {
                    int qID = idSync.qId;
                    Debug.Log($"Updating directly-created questionnaire with qID {qID} to have {newPNum} forms.");
                    
                    Undo.DestroyObjectImmediate(parentTransform.gameObject);
                    QuestionnaireEditorManager.CreateQuestionnaireDirectly(qID, newPNum);
                }
            }
            
            Debug.LogWarning($"Skipping questionnaire update for {parentTransform.name} as dependent classes are not available to this script.");
        }
    }
}
