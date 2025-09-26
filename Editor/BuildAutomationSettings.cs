using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;
using System.IO;

namespace LM.BuildAutomation.Editor
{
    [CreateAssetMenu(fileName = "BuildAutomationSettings", menuName = "Build/BuildAutomationSettings")]
    [CustomEditor(typeof(BuildAutomationSettings))]
    public class BuildAutomationSettings : ScriptableObject
    {
        public enum BuildMode
        {
            Simple,      // 간단 모드 - 빌드 타겟만 설정
            Profile      // 프로파일 모드 - 빌드 프로필 설정
        }

        [field: SerializeField] public BuildMode CurrentBuildMode = BuildMode.Profile;

        [Space(10)]
        [Header("빌드/실행 기본값")]
        [field: SerializeField] public string BuildTargetPath;
        [field: SerializeField] public string ExeName;
        [field: SerializeField] public int WindowWidth = 800;
        [field: SerializeField] public int WindowHeight = 600;
        [field: SerializeField] [Range(1, 4)] public int PlayerCount = 4;

        [Header("프로필")]
        [field: SerializeField] public BuildProfile BuildProfile;

        [Header("명령줄 인자 템플릿(멀티 인스턴스)")]
        [field: SerializeField] public int BasePort = 7777;
        [field: SerializeField] public string NicknameBase = "Player";
        [field: SerializeField][TextArea] public string ArgsTemplate = "--instance {index} --port {port} --nickname {nickname}-{index} -screen-width {width} -screen-height {height}";

        private const string DEFAULT_ASSET_PATH = "Assets/Settings/BuildAutomation/BuildAutomationSettings.asset";

        public static BuildAutomationSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BuildAutomationSettings>(DEFAULT_ASSET_PATH);
            if (settings == null)
            {
                var dir = Path.GetDirectoryName(DEFAULT_ASSET_PATH);
                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                settings = ScriptableObject.CreateInstance<BuildAutomationSettings>();
                settings.ApplyDynamicDefaults();
                AssetDatabase.CreateAsset(settings, DEFAULT_ASSET_PATH);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        /// <summary>
        /// 비어있거나 유효하지 않은 필드 기본값 보정
        /// </summary>
        public void ApplyDynamicDefaults()
        {
            if (string.IsNullOrEmpty(ExeName))
                ExeName = Application.productName;

            if (string.IsNullOrEmpty(BuildTargetPath))
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var ret = Path.Combine(projectRoot, "Builds");
                BuildTargetPath = ret;
            }

            if (WindowWidth <= 0)
                WindowWidth = 800;
            if (WindowHeight <= 0)
                WindowHeight = 600;
            if (PlayerCount <= 0)
                PlayerCount = 4;

            if (BasePort <= 0)
                BasePort = 7777;
            if (string.IsNullOrEmpty(NicknameBase))
                NicknameBase = "Player";
            if (string.IsNullOrEmpty(ArgsTemplate))
                ArgsTemplate = "--instance {index} --port {port} --nickname {nickname}-{index} -screen-width {width} -screen-height {height}";
        }
    }
}
