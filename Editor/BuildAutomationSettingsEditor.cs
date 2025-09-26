using UnityEngine;
using UnityEditor;
using LM.BuildAutomation.Editor;
using System.IO;
using System.Linq;

namespace LM.BuildAutomation.Editor
{
    public static class Utils
    {
        /// <summary>
        /// 프로젝트 루트 폴더 경로 반환
        /// </summary>
        private static string GetProjectRoot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return projectRoot;
        }

        /// <summary>
        /// 절대 경로를 프로젝트 상대 경로로 변환
        /// </summary>
        private static string ToProject_RelativePath(string absolutePath)
        {
            var projectRoot = GetProjectRoot();
            if (absolutePath.StartsWith(projectRoot))
            {
                return absolutePath.Substring(projectRoot.Length + 1);
            }
            return absolutePath;
        }

        /// <summary>
        /// Note 
        /// - 위치가 제대로 지정돼지 않는 버그가 있어 하드코딩으로 Offset을 할당하도록 추가함.
        /// </summary>
        public static void DrawPathField_Picker(string label, SerializedProperty pathProp, float buttonTopOffset)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(pathProp, new GUIContent(label), true);

            float btnWidth = 30f;
            float btnHeight = EditorGUIUtility.singleLineHeight;

            Rect btnRect = GUILayoutUtility.GetRect(btnWidth, btnHeight,
                GUILayout.Width(btnWidth), GUILayout.Height(btnHeight));
            btnRect.y += buttonTopOffset;

            if (GUI.Button(btnRect, "..."))
            {
                string selected = EditorUtility.OpenFolderPanel(label, GetProjectRoot(), pathProp.stringValue);
                if (string.IsNullOrEmpty(selected) == false)
                {
                    string relative = ToProject_RelativePath(selected);
                    pathProp.stringValue = relative;
                    pathProp.serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    [CustomEditor(typeof(BuildAutomationSettings))]
    public class BuildAutomationSettingsEditor : UnityEditor.Editor
    {
        private static (string, string) SIMPLE_MODE_CONTENT = ("⚡ 간단 모드", "빌드 타겟만 설정하여 빠른 빌드 테스트를 제공합니다.");
        private static (string, string) PROFILE_MODE_CONTENT = ("🔧 프로필 모드", "빌드 프로필을 할당해야 합니다.");
        private static Color SIMPLE_MODE_COLOR = Color.green;
        private static Color PROFILE_MODE_COLOR = new Color(0.2f, 0.6f, 1f);
        private BuildAutomationSettings _settings;
        private void OnEnable()
        {
            _settings = (BuildAutomationSettings)target;
        }

        public override void OnInspectorGUI()
        {
            DrawModeSelectionTabs();
            EditorGUILayout.Space();

            switch (_settings.CurrentBuildMode)
            {
                case BuildAutomationSettings.BuildMode.Simple:
                    DrawSimpleModeUI();
                    break;
                case BuildAutomationSettings.BuildMode.Profile:
                    DrawProfileModeUI();
                    break;
            }

            if (GUI.changed)
                EditorUtility.SetDirty(_settings);
        }

        /// <summary>
        /// 모드 선택 탭 그리기
        /// </summary>
        private void DrawModeSelectionTabs()
        {
            var simpleModeContent = new GUIContent(SIMPLE_MODE_CONTENT.Item1, SIMPLE_MODE_CONTENT.Item2);
            var profileModeContent = new GUIContent(PROFILE_MODE_CONTENT.Item1, PROFILE_MODE_CONTENT.Item2);

            var tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fixedHeight = 30
            };

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var originColor = GUI.backgroundColor;

            if (_settings.CurrentBuildMode == BuildAutomationSettings.BuildMode.Simple)
            {
                GUI.backgroundColor = originColor;
                if (GUILayout.Button(simpleModeContent, tabStyle, GUILayout.ExpandWidth(true)))
                {
                    // 동일 모드 클릭 시 유지
                }
            }
            else
            {
                GUI.backgroundColor = SIMPLE_MODE_COLOR;
                if (GUILayout.Button(simpleModeContent, tabStyle, GUILayout.ExpandWidth(true)))
                    _settings.CurrentBuildMode = BuildAutomationSettings.BuildMode.Simple;
            }

            if (_settings.CurrentBuildMode == BuildAutomationSettings.BuildMode.Profile)
            {
                GUI.backgroundColor = originColor;
                if (GUILayout.Button(profileModeContent, tabStyle, GUILayout.ExpandWidth(true)))
                {
                    // 동일 모드 클릭 시 유지
                }
            }
            else
            {
                GUI.backgroundColor = PROFILE_MODE_COLOR;
                if (GUILayout.Button(profileModeContent, tabStyle, GUILayout.ExpandWidth(true)))
                    _settings.CurrentBuildMode = BuildAutomationSettings.BuildMode.Profile;
            }

            GUI.backgroundColor = originColor;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSimpleModeUI()
        {
            Internal_DrawCommonSettings();

            EditorGUILayout.Space();
            if (GUILayout.Button("빌드 및 실행 (간단 모드)", GUILayout.Height(30)))
            {
                BuildAutomation.SimpleBuild();
            }
        }

        private void DrawProfileModeUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            _settings.ExeName = EditorGUILayout.TextField("실행 파일명", _settings.ExeName);

            _settings.BuildProfile = (UnityEditor.Build.Profile.BuildProfile)EditorGUILayout.ObjectField(
                "빌드 프로필",
                _settings.BuildProfile,
                typeof(UnityEditor.Build.Profile.BuildProfile),
                false);

            Internal_DrawCommonSettings();

            EditorGUILayout.Space();
            if (GUILayout.Button("빌드 및 실행 (프로필)", GUILayout.Height(30)))
            {
                if (_settings.BuildProfile == null)
                {
                    UnityEngine.Debug.LogWarning("빌드 프로필이 지정되지 않았습니다.");
                    return;
                }
                BuildAutomation.BuildWithProfile(_settings.BuildProfile);
            }
        }

        private void Internal_DrawCommonSettings()
        {
            EditorGUILayout.Space();

            var buildPathProp = serializedObject.FindProperty("BuildTargetPath");
            Utils.DrawPathField_Picker("빌드 폴더 경로", buildPathProp, 38.0f);

            _settings.ExeName = EditorGUILayout.TextField("실행 파일명", _settings.ExeName);

            EditorGUILayout.LabelField("창 설정", EditorStyles.boldLabel);
            _settings.WindowWidth = EditorGUILayout.IntField("창 너비", _settings.WindowWidth);
            _settings.WindowHeight = EditorGUILayout.IntField("창 높이", _settings.WindowHeight);
            _settings.PlayerCount = EditorGUILayout.IntSlider("플레이어 수", _settings.PlayerCount, 1, 4);

            EditorGUILayout.Space();

            if (GUILayout.Button("모니터 해상도 맞춤"))
            {
                BuildAutomation.SetWindowSizeToMonitor();
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
