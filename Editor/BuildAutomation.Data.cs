using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace LM.BuildAutomation.Editor
{
    public partial class BuildAutomation
    {
        // --- Setting(SO) 래퍼
        private static BuildAutomationSettings _settings;
        public static BuildAutomationSettings Settings
        {
            get
            {
                _settings = _settings ?? BuildAutomationSettings.LoadOrCreate();
                return _settings;
            }
        }

        private static string BuildTargetPath
        {
            get => Settings.BuildTargetPath;
            set
            {
                Settings.BuildTargetPath = value;
                SaveSettings();
            }
        }

        private static string ExeName
        {
            get => Settings.ExeName;
            set
            {
                Settings.ExeName = value;
                SaveSettings();
            }
        }

        private static int WindowWidth
        {
            get => Settings.WindowWidth;
            set
            {
                Settings.WindowWidth = value;
                SaveSettings();
            }
        }

        private static int WindowHeight
        {
            get => Settings.WindowHeight;
            set
            {
                Settings.WindowHeight = value;
                SaveSettings();
            }
        }

        private static int PlayerCount
        {
            get => Settings.PlayerCount;
            set
            {
                Settings.PlayerCount = value;
                SaveSettings();
            }
        }

        private static void SaveSettings()
        {
            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssets();
        }

        // --- Win32 API

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SW_NORMAL = 1;
        private const int SM_CXSCREEN = 0;          // 화면 너비 메트릭스 상수
        private const int SM_CYSCREEN = 1;          // 화면 높이 메트릭스 상수

        // 창 위치 정보 - 에디터 UI에서 수정 가능
        private static Vector2Int[] _windowPositions = 
        {
            new Vector2Int(0, 0),      // 왼쪽 위
            new Vector2Int(800, 0),    // 오른쪽 위
            new Vector2Int(0, 600),    // 왼쪽 아래
            new Vector2Int(800, 600)   // 오른쪽 아래
        };

        /// <summary>
        /// 창 위치 계산
        /// </summary>
        /// <param name="halfWidth">창 너비 반 너비</param>
        /// <param name="halfHeight">창 높이 반 높이</param>
        /// <returns>창 위치 배열</returns>
        private static Vector2Int[] CreateWindowPositions(int halfWidth, int halfHeight)
        {
            var ret = new Vector2Int[4];    
            ret[0] = new Vector2Int(0, 0);
            ret[1] = new Vector2Int(halfWidth, 0);
            ret[2] = new Vector2Int(0, halfHeight);
            ret[3] = new Vector2Int(halfWidth, halfHeight);
            return ret;
        }

        /// <summary>
        /// 모니터 해상도에 따라 창 위치 자동 계산
        /// </summary>
        private static Vector2Int[] CalculateWindowPositions()
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // 작업 표시줄 높이 고려 (대략 40픽셀)
            int taskbarHeight = 40;
            int availableHeight = screenHeight - taskbarHeight;

            // 4분할 위치 계산
            int halfWidth = screenWidth / 2;
            int halfHeight = availableHeight / 2;

            var ret = CreateWindowPositions(halfWidth, halfHeight);
            return ret;
        }

        /// <summary>
        /// 모니터 해상도에 맞춰 창 크기 및 위치 설정
        /// </summary>
        public static void SetWindowSizeToMonitor()
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // 작업 표시줄 높이 고려
            int taskbarHeight = 40;
            int availableHeight = screenHeight - taskbarHeight;

            // 4분할 크기 설정
            WindowWidth = screenWidth / 2;
            WindowHeight = availableHeight / 2;

            _windowPositions = CalculateWindowPositions();
            Debug.Log($"모니터 해상도 맞춤: {screenWidth} x {screenHeight}, 창 크기: {WindowWidth}x{WindowHeight}");
        }
    }
}