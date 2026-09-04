using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class AgentBridge
{
    [Serializable]
    private class AgentRequest
    {
        public string id;
        public string command;
        public string requestedAt;
    }

    [Serializable]
    private class CompileError
    {
        public string file;
        public int line;
        public int column;
        public string message;
    }

    [Serializable]
    private class AgentResult
    {
        public string id;
        public string command;
        public string status;
        public string timestamp;
        public List<CompileError> compileErrors;
    }

    [Serializable]
    private class Heartbeat
    {
        public bool editorRunning;
        public string unityVersion;
        public string project;
        public bool isCompiling;
        public bool isPlaying;
        public string timestamp;
    }

    private static readonly string ProjectRoot;
    private static readonly string AgentDirectory;
    private static readonly string RequestPath;
    private static readonly string ResultPath;
    private static readonly string HeartbeatPath;

    private static double _nextHeartbeatTime;
    private static double _nextRequestCheckTime;

    private static AgentRequest _pendingRequest;
    private static readonly List<CompileError> CompileErrors = new();

    static AgentBridge()
    {
        ProjectRoot = Directory.GetParent(Application.dataPath)?.FullName
                      ?? throw new InvalidOperationException(
                          "Could not determine Unity project root.");

        AgentDirectory = Path.Combine(ProjectRoot, ".agent");
        RequestPath = Path.Combine(AgentDirectory, "request.json");
        ResultPath = Path.Combine(AgentDirectory, "result.json");
        HeartbeatPath = Path.Combine(AgentDirectory, "heartbeat.json");

        Directory.CreateDirectory(AgentDirectory);

        // Domain Reload時の二重登録を避ける
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;

        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationStarted += OnCompilationStarted;

        CompilationPipeline.assemblyCompilationFinished
            -= OnAssemblyCompilationFinished;
        CompilationPipeline.assemblyCompilationFinished
            += OnAssemblyCompilationFinished;

        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        CompilationPipeline.compilationFinished += OnCompilationFinished;

        WriteHeartbeat();

        Debug.Log("[AgentBridge] Initialized.");
    }

    private static void OnEditorUpdate()
    {
        double now = EditorApplication.timeSinceStartup;

        // 2秒ごとにHeartbeat更新
        if (now >= _nextHeartbeatTime)
        {
            _nextHeartbeatTime = now + 2.0;
            WriteHeartbeat();
        }

        // request.jsonは0.25秒ごとに確認
        if (now >= _nextRequestCheckTime)
        {
            _nextRequestCheckTime = now + 0.25;
            CheckForRequest();
        }
    }

    private static void WriteHeartbeat()
    {
        try
        {
            var heartbeat = new Heartbeat
            {
                editorRunning = true,
                unityVersion = Application.unityVersion,
                project = Path.GetFileName(ProjectRoot),
                isCompiling = EditorApplication.isCompiling,
                isPlaying = EditorApplication.isPlaying,
                timestamp = DateTimeOffset.Now.ToString("o")
            };

            WriteJsonAtomic(
                HeartbeatPath,
                JsonUtility.ToJson(heartbeat, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"[AgentBridge] Failed to write heartbeat: {ex.Message}");
        }
    }

    private static void CheckForRequest()
    {
        // すでに処理中なら新しい要求を受け付けない
        if (_pendingRequest != null)
            return;

        // コンパイル中には要求を開始しない
        if (EditorApplication.isCompiling)
            return;

        if (!File.Exists(RequestPath))
            return;

        try
        {
            string json = File.ReadAllText(RequestPath);

            if (string.IsNullOrWhiteSpace(json))
                return;

            AgentRequest request =
                JsonUtility.FromJson<AgentRequest>(json);

            if (request == null ||
                string.IsNullOrWhiteSpace(request.id) ||
                string.IsNullOrWhiteSpace(request.command))
            {
                Debug.LogWarning(
                    "[AgentBridge] Invalid request.json.");
                return;
            }

            switch (request.command)
            {
                case "compile_status":
                    StartCompileValidation(request);
                    break;

                case "run_editmode_tests":
                    WriteUnsupportedResult(request);
                    break;

                case "run_playmode_tests":
                    WriteUnsupportedResult(request);
                    break;

                default:
                    WriteUnsupportedResult(request);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[AgentBridge] Failed to process request: {ex}");
        }
    }

    private static void StartCompileValidation(AgentRequest request)
    {
        _pendingRequest = request;
        CompileErrors.Clear();

        Debug.Log(
            $"[AgentBridge] Compile validation requested: {request.id}");

        // 現在のソースコードを必ず再コンパイルして検証する。
        CompilationPipeline.RequestScriptCompilation();
    }

    private static void OnCompilationStarted(object context)
    {
        if (_pendingRequest == null)
            return;

        CompileErrors.Clear();

        Debug.Log(
            $"[AgentBridge] Compilation started: {_pendingRequest.id}");
    }

    private static void OnAssemblyCompilationFinished(
        string assemblyPath,
        CompilerMessage[] messages)
    {
        if (_pendingRequest == null)
            return;

        foreach (CompilerMessage compilerMessage in messages)
        {
            if (compilerMessage.type != CompilerMessageType.Error)
                continue;

            CompileErrors.Add(new CompileError
            {
                file = compilerMessage.file,
                line = compilerMessage.line,
                column = compilerMessage.column,
                message = compilerMessage.message
            });
        }
    }

    private static void OnCompilationFinished(object context)
    {
        if (_pendingRequest == null)
            return;

        AgentRequest completedRequest = _pendingRequest;

        bool success = CompileErrors.Count == 0;

        var result = new AgentResult
        {
            id = completedRequest.id,
            command = completedRequest.command,
            status = success ? "success" : "failed",
            timestamp = DateTimeOffset.Now.ToString("o"),
            compileErrors = new List<CompileError>(CompileErrors)
        };

        try
        {
            WriteJsonAtomic(
                ResultPath,
                JsonUtility.ToJson(result, true));

            DeleteRequestIfMatching(completedRequest.id);

            Debug.Log(
                success
                    ? $"[AgentBridge] Validation succeeded: {completedRequest.id}"
                    : $"[AgentBridge] Validation failed with {CompileErrors.Count} error(s): {completedRequest.id}");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[AgentBridge] Failed to write result: {ex}");
        }
        finally
        {
            _pendingRequest = null;
            CompileErrors.Clear();
        }
    }

    private static void WriteUnsupportedResult(AgentRequest request)
    {
        var result = new AgentResult
        {
            id = request.id,
            command = request.command,
            status = "failed",
            timestamp = DateTimeOffset.Now.ToString("o"),
            compileErrors = new List<CompileError>()
        };

        WriteJsonAtomic(
            ResultPath,
            JsonUtility.ToJson(result, true));

        DeleteRequestIfMatching(request.id);

        Debug.LogWarning(
            $"[AgentBridge] Command is not implemented yet: {request.command}");
    }

    private static void DeleteRequestIfMatching(string requestId)
    {
        if (!File.Exists(RequestPath))
            return;

        try
        {
            string json = File.ReadAllText(RequestPath);
            AgentRequest current =
                JsonUtility.FromJson<AgentRequest>(json);

            if (current != null && current.id == requestId)
                File.Delete(RequestPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"[AgentBridge] Could not delete request.json: {ex.Message}");
        }
    }

    private static void WriteJsonAtomic(
        string destinationPath,
        string contents)
    {
        string tempPath = destinationPath + ".tmp";

        File.WriteAllText(tempPath, contents);

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        File.Move(tempPath, destinationPath);
    }
}