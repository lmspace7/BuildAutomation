using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using System.Text;

namespace LM.BuildAutomation.Editor
{
    public partial class BuildAutomation
    {
        private static bool _isDebugProcessFinding = false;
        
        [MenuItem("에디터툴/Build Automation/Setting")]
        public static void FocusSetting()
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = Settings;
        }

        [MenuItem("에디터툴/Build Automation/Run SimpeMode BuildAndRun")]
        public static void RunSimpleModeBuild()
        {
            SimpleBuild();
        }

        [MenuItem("에디터툴/Build Automation/Run ProfileModeBuild")]
        public static void RunProfileModeBuild()
        {
            BuildWithProfile(Settings.BuildProfile);
        }


        private static string _pendingExecutablePath = "";
        private static string _lastExePath = "";
        private static EditorApplication.CallbackFunction _launchInstancesCallback;
        private static EditorApplication.CallbackFunction _arrangeWindowsCallback;
        
        static BuildAutomation()
        {
            _launchInstancesCallback = () =>
            {
                if (string.IsNullOrEmpty(_pendingExecutablePath) == false)
                {
                    LaunchMultipleInstances(_pendingExecutablePath);
                    _pendingExecutablePath = "";
                }
            };

            _arrangeWindowsCallback = () => ArrangeWindowsWithRetry();
        }

        /// <summary>
        /// 현재 설정으로 빌드 수행 및 성공 시 다중 인스턴스 실행
        /// </summary>
        public static void SimpleBuild()
        {
            UnityEngine.Debug.Log("빌드 시작...");

            string outputDir = ResolveOutputDirectory(BuildTargetPath);
            if (Directory.Exists(outputDir) == false)
            {
                Directory.CreateDirectory(outputDir);
                UnityEngine.Debug.Log($"빌드 폴더 생성됨: {outputDir}");
            }

            // 빌드 실행
            string exePath = Path.Combine(outputDir, ExeName + ".exe");
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = GetEnabledScenes();
            buildOptions.locationPathName = exePath;
            buildOptions.target = BuildTarget.StandaloneWindows64;
            buildOptions.options = BuildOptions.None;

            var buildReport = BuildPipeline.BuildPlayer(buildOptions);

            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log("빌드 성공!");
                CleanupDelayedCallbacks();
                _pendingExecutablePath = exePath;
                _lastExePath = exePath;
                EditorApplication.delayCall += _launchInstancesCallback;
            }
            else
            {
                UnityEngine.Debug.LogError("빌드 실패!");
            }
        }

        /// <summary>
        /// 프로필 기반으로 빌드 수행 및 성공 시 다중 인스턴스 실행
        /// </summary>
        public static void BuildWithProfile(BuildProfile profile)
        {
            if (profile == null)
            {
                UnityEngine.Debug.LogError("빌드 프로필이 비어 있습니다.");
                return;
            }

            UnityEngine.Debug.Log("프로필 빌드 시작...");

            var options = new BuildPlayerWithProfileOptions();
            options.buildProfile = profile;
            string outputDir = ResolveOutputDirectory(BuildTargetPath);
            string exePath = Path.Combine(outputDir, ExeName + ".exe");
            options.locationPathName = exePath;
            options.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                CleanupDelayedCallbacks();
                _pendingExecutablePath = exePath;
                _lastExePath = exePath;
                EditorApplication.delayCall += _launchInstancesCallback;
            }
            else
            {
                UnityEngine.Debug.LogError("빌드 실패!");
            }
        }

        private static string ResolveOutputDirectory(string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath) == true)
            {
                var ret = configuredPath;
                return ret;
            }
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string combined = Path.GetFullPath(Path.Combine(projectRoot, configuredPath));
            var result = combined;
            return result;
        }

        private static void CleanupDelayedCallbacks()
        {
            EditorApplication.delayCall -= _launchInstancesCallback;
            EditorApplication.delayCall -= _arrangeWindowsCallback;
            _pendingExecutablePath = "";
        }

        /// <summary>
        /// 기존 인스턴스 종료 후 플레이어 수 만큼 새 인스턴스 실행
        /// </summary>
        private static void LaunchMultipleInstances(string exePath)
        {
            if (File.Exists(exePath) == false)
            {
                UnityEngine.Debug.LogError($"실행 파일을 찾을 수 없습니다: {exePath}");
                return;
            }

            // 기존 프로세스 종료
            _lastExePath = exePath;
            KillExistingProcesses(exePath);

            for (int i = 0; i < PlayerCount; i++)
                LaunchInstance(exePath, i);

            EditorApplication.delayCall += _arrangeWindowsCallback;
        }

        /// <summary>
        /// 단일 인스턴스 실행 및 명령줄 인자 전달
        /// </summary>
        private static void LaunchInstance(string executablePath, int instanceIndex)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = executablePath;
                startInfo.Arguments = CMDLineArgsBuilder.Build(Settings, instanceIndex);
                startInfo.UseShellExecute = false;

                Process process = Process.Start(startInfo);

                UnityEngine.Debug.Log($"인스턴스 {instanceIndex + 1} 실행됨");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"인스턴스 {instanceIndex + 1} 실행 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 창 배치 재시도 로직(Windows 전용)
        /// - 메인 윈도 핸들 지연 생성 가능성 고려, 지정 횟수/간격 재시도 수행
        /// </summary>
        private static void ArrangeWindowsWithRetry()
        {
#if !UNITY_EDITOR_WIN
            Debug.LogWarning("윈도우 전용 기능: 창 배치는 Windows 에디터에서만 지원됩니다.");
            return;
    #endif
            Task.Run(async () =>
            {
                int maxRetries = 10;
                int retryDelay = 1000; // 1초

                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        await Task.Delay(retryDelay);

                        // 실행 중인 프로세스 검색(전체 경로 기준)
                        if (string.IsNullOrEmpty(_lastExePath) == true)
                        {
                            UnityEngine.Debug.Log($"재시도 {attempt + 1}/{maxRetries}: 실행 파일 경로 비어 있음");
                            continue;
                        }

                        if (_isDebugProcessFinding == true)
                        {
                            UnityEngine.Debug.Log($"[Debug] 타겟 경로 Exe 파일: {_lastExePath}");
                            DebugLogTargetSearch(_lastExePath);
                        }

                        Process[] processes = FindProcess(_lastExePath);

                        if (processes.Length == 0)
                        {
                            UnityEngine.Debug.Log($"재시도 {attempt + 1}/{maxRetries}: 프로세스 없음");
                            continue;
                        }

                        // 모든 프로세스 MainWindowHandle 보유 여부 확인
                        int validWindows = 0;
                        for (int i = 0; i < processes.Length; i++)
                        {
                            if (processes[i].MainWindowHandle != IntPtr.Zero)
                                validWindows++;
                        }

                        if (validWindows < PlayerCount)
                        {
                            UnityEngine.Debug.Log($"재시도 {attempt + 1}/{maxRetries}: 유효 창 {validWindows}/{PlayerCount}");
                            continue;
                        }

                        // 창 위치 조정 실행, 만약을 위해 지연수행 1번더
                        EditorApplication.delayCall += () => ArrangeWindows();
                        UnityEngine.Debug.Log($"창 배치 성공 (재시도 {attempt + 1}회)");
                        await Task.Delay(1200);
                        EditorApplication.delayCall += () => ArrangeWindows();
                        return;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"창 배치 재시도 {attempt + 1} 실패: {e.Message}");
                    }
                }

                UnityEngine.Debug.LogError($"창 배치 최종 실패: {maxRetries}회 재시도 후 포기");
            });
        }

        /// <summary>
        /// 실행 중인 인스턴스 창을 화면 4분할 위치로 이동/크기 지정
        /// </summary>
        private static void ArrangeWindows()
        {
#if !UNITY_EDITOR_WIN
            Debug.LogWarning("윈도우 전용 기능: 창 배치는 Windows 에디터에서만 지원됩니다.");
            return;
#endif
            try
            {
                // 에디터 UI에서 지정한 사용자 좌표 사용
                Vector2Int[] calculatedPositions = _windowPositions;
                Process[] processes = FindProcess(_lastExePath);
                UnityEngine.Debug.Log($"발견된 프로세스 수: {processes.Length}");
                LoopForEachProcess(processes, calculatedPositions);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"창 배치 실패: {e.Message}");
            }
        }

        private static void LoopForEachProcess(Process[] processes, Vector2Int[] calculatedPositions)
        {
            int min = Math.Min(processes.Length, PlayerCount);
            for (int i = 0; i < min; i++)
            {
                try
                {
                    IntPtr hWnd = processes[i].MainWindowHandle;
                    if (hWnd == IntPtr.Zero)
                        continue;

                    Vector2Int position = calculatedPositions[i];
                    bool ok = SetWindowPos(hWnd, IntPtr.Zero, position.x, position.y,
                                WindowWidth, WindowHeight, SWP_NOZORDER | SWP_NOACTIVATE);
                    if (ok == false)
                    {
                        UnityEngine.Debug.LogError($"창 {i + 1} 위치 조정 실패(SetWindowPos false): ({position.x}, {position.y})");
                    }
                    else
                    {
                        ShowWindow(hWnd, SW_NORMAL);
                        UnityEngine.Debug.Log($"창 {i + 1} 위치 조정: ({position.x}, {position.y})");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"창 {i + 1} 위치 조정 실패: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Build Settings에서 enabled 씬만 수집
        /// </summary>
        private static string[] GetEnabledScenes()
        {
            var list = new List<string>();
            var editorScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < editorScenes.Length; i++)
            {
                if (editorScenes[i].enabled == true)
                    list.Add(editorScenes[i].path);
            }
            var ret = list.ToArray();
            return ret;
        }

        /// <summary>
        /// exe 전체 경로와 일치하는 기존 프로세스 종료 - 잔존 프로세스 종료 목적
        /// Note - Process.Kill() 함수는 종료를 하는 함수지만 안전한 방법은 아님, 추후 개선 필요
        /// </summary>
        private static void KillExistingProcesses(string targetPath)
        {
            try
            {
                Process[] processes = FindProcess(targetPath);
                for (int i = 0; i < processes.Length; i++)
                {
                    processes[i].Kill();
                    processes[i].WaitForExit(1000);
                }
                if (processes.Length > 0)
                    UnityEngine.Debug.Log($"기존 프로세스 {processes.Length}개 종료");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"기존 프로세스 종료 실패: {e.Message}");
            }
        }

        /// <summary>
        /// exe 전체 경로와 일치하는 프로세스 선별
        /// </summary>
        private static Process[] FindProcess(string targetPath)
        {
            try
            {
                string normalizedTarget = NormalizePathSeperator(targetPath);
                string fileName = Path.GetFileNameWithoutExtension(normalizedTarget);
                Process[] processes = Process.GetProcessesByName(fileName);
                var list = new List<Process>();

                if (_isDebugProcessFinding)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"[Debug] 후보 프로세스 수: {processes.Length}, 파일명 기준: '{fileName}'");
                    for (int i = 0; i < processes.Length; i++)
                    {
                        try
                        {
                            string mm = processes[i].MainModule != null ? processes[i].MainModule.FileName : "<null MainModule>";
                            sb.AppendLine($"  - PID={processes[i].Id}, Name={processes[i].ProcessName}, MainModule={mm}");
                        }
                        catch (Exception e)
                        {
                            sb.AppendLine($"  - PID={processes[i].Id}, Name={processes[i].ProcessName}, MainModule=<access denied> ({e.GetType().Name})");
                        }
                    }
                    UnityEngine.Debug.Log(sb.ToString());
                }

                for (int i = 0; i < processes.Length; i++)
                {
                    // 특정 프로세스는 MainModule에 접근시 권한 때문에 예외가 발생할수 있음.
                    try
                    {
                        string procPath = NormalizePathSeperator(processes[i].MainModule.FileName);
                        // 대소문자 구분없이 비교(정규화된 경로)
                        bool same = string.Equals(procPath, normalizedTarget, StringComparison.OrdinalIgnoreCase);

                        if (same)
                        {
                            list.Add(processes[i]);
                        }
                    }
                    catch
                    {
                    }
                }
                var ret = list.ToArray();
                return ret;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"프로세스 검색 실패: {e.Message}");
                var ret = Array.Empty<Process>();
                return ret;
            }
        }

        /// <summary>
        /// 운영체제 기반 경로 구분자 정규화
        /// </summary>
        private static string NormalizePathSeperator(string path)
        {
            if (string.IsNullOrEmpty(path) == true)
            {
                var ret = string.Empty;
                return ret;
            }
            string replaced = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string full = replaced;
            try
            {
                full = Path.GetFullPath(replaced);
            }
            catch
            {
            }
            string trimmed = full.TrimEnd(Path.DirectorySeparatorChar);
            var result = trimmed;
            return result;
        }

        /// <summary>
        /// 디버깅용 - 대상 검색 로그 출력
        /// </summary>
        private static void DebugLogTargetSearch(string targetPath)
        {
            try
            {
                string expectName = Path.GetFileNameWithoutExtension(targetPath);
                var processes = Process.GetProcessesByName(expectName);
                var sb = new StringBuilder();
                sb.AppendLine($"[Debug] 대상 검색: expectName='{expectName}', targetPath='{targetPath}', candidates={processes.Length}");
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        string mm = processes[i].MainModule != null ? processes[i].MainModule.FileName : "<null MainModule>";
                        sb.AppendLine($"  - PID={processes[i].Id}, Name={processes[i].ProcessName}, MainModule={mm}");
                    }
                    catch (Exception e)
                    {
                        sb.AppendLine($"  - PID={processes[i].Id}, Name={processes[i].ProcessName}, MainModule=<access denied> ({e.GetType().Name})");
                    }
                }
                UnityEngine.Debug.Log(sb.ToString());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"대상 검색 로그 실패: {e.Message}");
            }
        }
    }
}